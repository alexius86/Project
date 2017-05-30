import asyncio
import base64
import json
from datetime import datetime

import aiohttp_cors
from aiohttp import web
from aiohttp_session import get_session, session_middleware
from aiohttp_session.cookie_storage import EncryptedCookieStorage
from cryptography import fernet

from drw.utils.loggers import logging
from solutions.construction.models.model import OWSites, OWSlabs, OWScanTypes, OWScan


async def auth_handler(request):
    result = dict(await request.post())

    for key, value in result.items():
        if value == 'null' or value == '':
            result[key] = None
    try:
        token = await request.app.ow_app.create_token(**result)
    except Exception as e:
        error_msg = str(e)
        return web.Response(text=json.dumps({'error': error_msg}), content_type='json')

    session = await get_session(request)
    session['token'] = token.decode('utf-8') # sets the application token to the session of aiohttp

    return web.Response(
        text=json.dumps({'status':'ok'})
    )

async def ping(request):
    return web.json_response(dict(status='ok'))

async def get_user_info(request, session):
    """ This is a check to make sure we can authenticate """
    token = session.get('token')
    if token:
        session_obj = await request.app.ow_app.authenticate_token(token)
        if session_obj:
            user = await session_obj.user
            return user
    return False

class User(web.View):
    async def get(self):
        """
        If authenticated, get get the user info.
        {
            "id": int,
            "username": someUser
        }
        """
        session = await get_session(self.request)
        user = await get_user_info(self.request, session)
        if user:

            current_user = dict(
                username = await user.username,
                email = await user.email,
                first_name = await user.first_name,
                last_name = await user.last_name
            )
            return web.json_response(current_user)
        else:
            raise web.HTTPForbidden(reason='please authenticate at %s/auth' % (self.request.host))


def transform(items_dict):
    for attr, value in items_dict.items():
        if type(value) is dict:
            temp_list = []
            for k, v in value.items():
                temp_list.append(transform(v))
            items_dict[attr] = temp_list
    return items_dict

# def slab_dict_to_list(slab):
#     temp_list = []
#     for scan in slab['scans'].values():
#         temp_list.append(scan)
#     slab['scans'] = temp_list
#     return slab
#
# def site_dict_to_list(site):
#     temp_list = []
#     for slab in site['slabs'].values():
#         temp_list.append(slab_dict_to_list(slab))
#     site['slabs'] = temp_list
#     return site
#
# def sites_dict_to_list(sites_dict):
#     temp_list = []
#     for site in sites_dict.values():
#         temp_list.append(site_dict_to_list(site))
#     return temp_list


class Sites(web.View):

    async def get(self):
        session = await get_session(self.request)
        user = await get_user_info(self.request, session)
        if user:
            user_model = await user.models
            construction_model = user_model['construction']
            sites_json = await construction_model._to_dict()
            # temp_list = []
            sites_json_to_list = transform(sites_json)

            return web.json_response(sites_json_to_list)
        else:
            raise web.HTTPForbidden(reason='please authenticate at %s/auth' % (self.request.host))

    async def post(self):
        data = await self.request.post()
        name = data.get('name')
        description = data.get('description')

        session = await get_session(self.request)
        user = await get_user_info(self.request, session)
        if user:
            construction_model = (await user.models)['construction']
            site = await self.request.app.ow_app.create(
                OWSites.__name__,
                name = name,
                description = description
            )
            sites = await construction_model.getattr('sites')
            name = name.replace(' ', '_')
            sites[name] = site

            await construction_model.setattr('sites', sites)

            # update the model
            await self.request.app.ow_app.update(construction_model)

            return web.json_response(data=dict(status='ok'))
        else:
            raise web.HTTPForbidden(reason='please authenticate at %s/auth' % (self.request.host))

    async def delete(self):

        session = await get_session(self.request)
        user = await get_user_info(self.request, session)

        if user:
            construction_model = (await user.models)['construction']
            await construction_model.setattr('sites', {})
            # update the model
            await self.request.app.ow_app.update(construction_model)
            return web.json_response(data=dict(status='ok'))
        else:
            raise web.HTTPForbidden(reason='please authenticate at %s/auth' % (self.request.host))

class Site(web.View):
    """ This endpoint we will get specific site id's """
    async def get(self):
        site_id = self.request.match_info.get('site_id')
        session = await get_session(self.request)
        user = await get_user_info(self.request, session)
        if user:
            # Now we are going to try to
            user_model = await user.models
            construction_model = user_model['construction']
            sites = await construction_model.sites

            target_site = sites[site_id]     # nsproxy
            target_site = await target_site._to_dict()

            if target_site:
                return web.json_response(transform(target_site))
            else:
                #NO TARGET SITE
                return web.HTTPBadRequest(text="Site '%s' not found. Please visit /sites/ to get a full list of sites" % site_id)
        else:
            return web.HTTPForbidden(reason='please authenticate at %s/auth' % (self.request.host))

    async def post(self):
        """ This endpoint modifies a site slab """
        site_id = self.request.match_info.get('site_id')
        data = await self.request.post()

        session = await get_session(self.request)
        user = await get_user_info(self.request, session)
        if user:
            user_model = await user.models
            construction_model = user_model['construction']

            target_site = await self.request.app.ow_app.get(OWSites.__name__, site_id)

            name = data.get('name').replace(' ','_')
            if data.get('name'):
                await target_site.setattr('name',name)
            if data.get('description'):
                await target_site.setattr('description', data.get('description'))

            # update the model
            await self.request.app.ow_app.update(construction_model)

            # TODO: finish a good response
            return web.json_response(data=dict(status='ok'))
        else:
            raise web.HTTPForbidden(reason='please authenticate at %s/auth' % (self.request.host))

    async def delete(self):
        """ This endpoint to delete an item """
        print('deleting a site')
        site_id = self.request.match_info.get('site_id')

        session = await get_session(self.request)
        user = await get_user_info(self.request, session)
        if user:
            user_model = await user.models
            construction_model = user_model['construction']

            await construction_model.setattr('sites', {})
            # update the model
            await self.request.app.ow_app.update(construction_model)

            return web.json_response(data=dict(status='ok'))

        else:
            raise web.HTTPForbidden(reason='please authenticate at %s/auth' % (self.request.host))

async def get_construction_model(user_model):
    constuction_model = await user_model.get('construction')
    if constuction_model:
        return constuction_model

async def get_all_sites(user_model):
    construction_model = await get_construction_model(user_model)
    all_sites = await construction_model.sites




class Slabs(web.View):
    async def get(self):
        try:
            site_id = self.request.match_info.get('site_id')

            session = await get_session(self.request)
            user = await get_user_info(self.request, session)
            if user:
                try:
                    user_model = await user.models
                    construction_model = user_model['construction']

                    sites = await construction_model.sites

                    target_site = sites[site_id]     # nsproxy
                    target_site_json = await target_site._to_dict()

                    return web.json_response(transform(target_site_json)['slabs'])
                except Exception as e:
                    return web.json_response(data=dict(status='error', msg=str(e) + " not found"))
            else:
                raise web.HTTPForbidden(reason='please authenticate at %s/auth' % (self.request.host))
        except Exception as e:
            print(e)
            print('check it ')

    async def post(self):
        site_id = self.request.match_info.get('site_id')
        data = await self.request.post()

        session = await get_session(self.request)
        user = await get_user_info(self.request, session)
        if user:
            user_model = await user.models
            construction_model = user_model['construction']
            sites = await construction_model.sites

            target_site = sites[site_id]
            # target_site = sites[site_id]     # nsproxy
            if target_site:
                try:
                    target_site_slabs = await target_site.getattr('slabs')

                    new_slab = await self.request.app.ow_app.create(
                        OWSlabs.__name__,
                        name = data.get('name'),
                        timestamp= datetime.now(),
                        description = data.get('description')
                    )
                    name = data.get('name')
                    name = name.replace(' ','_')
                    target_site_slabs[name] = new_slab
                    # await construction_model.setattr('')
                    await target_site.setattr('slabs', target_site_slabs)
                    await self.request.app.ow_app.update(target_site)

                    sites[site_id] = target_site
                    await construction_model.setattr('sites', sites)
                    # target_site_slabs[new_slab._key] = new_slab

                    # update the model
                    await self.request.app.ow_app.update(construction_model)

                    print('check here')
                    return web.json_response(data=dict(status='ok', slab_id=new_slab._key))
                except Exception as e:
                    # Return the error
                    return web.json_response(data=dict(status='error', msg=e))
            else:
                return web.json_response(data=dict(status='error', msg='You have no site with the id of \'%s\'' % site_id))
        else:
            raise web.HTTPForbidden(reason='please authenticate at %s/auth' % (self.request.host))

    async def delete(self):
        """ This endpoint to delete an item """
        print('deleting slabs')
        site_id = self.request.match_info.get('site_id')
        session = await get_session(self.request)
        user = await get_user_info(self.request, session)
        if user:
            user_model = await user.models
            construction_model = user_model['construction']

            sites = await construction_model.sites
            site_proxy = sites[site_id]
            await site_proxy.setattr('slabs',{})
            # Delete and update the sites model
            await self.request.app.ow_app.update(construction_model)

            return web.json_response(data=dict(status='ok'))

        else:
            raise web.HTTPForbidden(reason='please authenticate at %s/auth' % (self.request.host))
class Slab(web.View):
    async def get(self):

        site_id = self.request.match_info.get('site_id')
        slab_id = self.request.match_info.get('slab_id')


        session = await get_session(self.request)
        user = await get_user_info(self.request, session)
        if user:
            user_model = await user.models
            construction_model = user_model['construction']

            sites = await construction_model.sites

            target_site = sites[site_id]     # nsproxy
            # slab
            slabs_dict = await target_site.slabs

            target_slab_json = await slabs_dict[slab_id]._to_dict()
            # slab
            return web.json_response(transform(target_slab_json))
        else:
            raise web.HTTPForbidden(reason='please authenticate at %s/auth' % (self.request.host))
    async def delete(self):

        site_id = self.request.match_info.get('site_id')
        slab_id = self.request.match_info.get('slab_id')


        session = await get_session(self.request)
        user = await get_user_info(self.request, session)
        if user:
            user_model = await user.models
            construction_model = user_model['construction']
            sites = await construction_model.sites
            target_site = sites[site_id]     # nsproxy
            # slab
            slabs_dict = await target_site.slabs
            slabs_dict.pop(slab_id)
            await target_site.setattr('slabs', slabs_dict)
            await self.request.app.ow_app.update(target_site)
            await self.request.app.ow_app.update(construction_model)


            return web.json_response(data=dict(status='ok'))

        else:
            raise web.HTTPForbidden(reason='please authenticate at %s/auth' % (self.request.host))


    async def post(self):
        site_id = self.request.match_info.get('site_id')
        slab_id = self.request.match_info.get('slab_id')
        session = await get_session(self.request)
        user = await get_user_info(self.request, session)
        data = await self.request.post()
        if user:
            user_model = await user.models
            construction_model = user_model['construction']

            sites = await construction_model.sites

            target_site = sites[site_id]     # nsproxy

            slabs_dict = await target_site.slabs

            target_slab_proxy = slabs_dict[slab_id]

            #upload one at a time

            new_scan = await self.request.app.ow_app.create(
                OWScan.__name__,
                io_config = await user.s3_config,
                scan_type = OWScanTypes[data.get('scan_type')],
                timestamp= datetime.now(),
                longitude = float(data.get('longitude')),
                latitude = float(data.get('latitude')),
                name = data.get('file_id'),
                url=f'{self.request.host}/v1/sites/{site_id}/{slab_id}/{data.get("file_id")}/download'
            )


            raw_data = data.get('raw_data').file

            # with new_scan.open() as file:
            #     log.debug('Writing to ceph.')

            # with raw_data as raw_bytes:
            #     await new_scan.write(raw_bytes)


            await new_scan.write(raw_data)
            name = data.get('file_id')
            name = name.replace(' ', '_')
            current_scan_dict = await target_slab_proxy.getattr('scans')
            current_scan_dict[name] = new_scan
            await target_slab_proxy.setattr('scans', current_scan_dict)
            await self.request.app.ow_app.update(target_slab_proxy)
            await self.request.app.ow_app.update(target_site)
            await self.request.app.ow_app.update(construction_model)
            # return web.json_response(await new_scan._to_dict())
            return web.json_response(data=dict(status='ok'))

        else:
            raise web.HTTPForbidden(reason='please authenticate at %s/auth' % (self.request.host))
    # async def put(self):
    #     """ Update a slab """
    #     site_id = self.request.match_info.get('site_id')
    #     slab_id = self.request.match_info.get('slab_id')

class Scans(web.View):
    """

    This is the view to get a list of scans for a slab
    '/v1/sites/{site_id:\w+}/{slab_id:\w+}/scans'

    """
    async def get(self):

        site_id = self.request.match_info.get('site_id')
        slab_id = self.request.match_info.get('slab_id')
        session = await get_session(self.request)
        user = await get_user_info(self.request, session)


        if user:
            user_model = await user.models
            construction_model = user_model['construction']

            sites = await construction_model.sites

            target_site = sites[site_id]     # nsproxy
            # slab
            slabs_dict = await target_site.slabs

            target_slab_json = (await slabs_dict[slab_id]._to_dict())
            # slab
            return web.json_response(transform(target_slab_json)['scans'])

        else:
            raise web.HTTPForbidden(reason='please authenticate at %s/auth' % (self.request.host))
    async def delete(self):
        site_id = self.request.match_info.get('site_id')
        slab_id = self.request.match_info.get('slab_id')
        session = await get_session(self.request)
        user = await get_user_info(self.request, session)


        if user:
            user_model = await user.models
            construction_model = user_model['construction']

            sites = await construction_model.sites

            target_site = sites[site_id]     # nsproxy
            # slab
            slabs_dict = await target_site.slabs
            slab_proxy = slabs_dict[slab_id]
            await slab_proxy.setattr('scans',{})
            await self.request.app.ow_app.update(slab_proxy)
            # await self.request.app.ow_app.update(target_site)
            # await self.request.app.ow_app.update(construction_model)

            # slab
            return web.json_response(status='okay')

        else:
            raise web.HTTPForbidden(reason='please authenticate at %s/auth' % (self.request.host))

class Scan(web.View):

    async def get(self):

        site_id = self.request.match_info.get('site_id')
        slab_id = self.request.match_info.get('slab_id')
        scan_id = self.request.match_info.get('scan_id')
        session = await get_session(self.request)
        user = await get_user_info(self.request, session)


        if user:
            user_model = await user.models
            construction_model = user_model['construction']

            sites = await construction_model.sites

            target_site = sites[site_id]     # nsproxy

            slabs_dict = await target_site.slabs
            slab_proxy = slabs_dict[slab_id]
            scan_dict = await slab_proxy.scans
            scan_proxy = scan_dict[scan_id]
            target_scan_json = await scan_proxy._to_dict()

            return web.json_response(transform(target_scan_json))

        else:
            raise web.HTTPForbidden(reason='please authenticate at %s/auth' % (self.request.host))

    async def delete(self):

        site_id = self.request.match_info.get('site_id')
        slab_id = self.request.match_info.get('slab_id')
        scan_id = self.request.match_info.get('scan_id')
        session = await get_session(self.request)
        user = await get_user_info(self.request, session)

        if user:
            user_model = await user.models
            construction_model = user_model['construction']

            sites = await construction_model.sites

            target_site = sites[site_id]  # nsproxy

            slabs_dict = await target_site.slabs
            slab_proxy = slabs_dict[slab_id]
            scan_dict = await slab_proxy.scans
            scan_dict.pop(scan_id)

            await slab_proxy.setattr('scans', scan_dict)
            await self.request.app.ow_app.update(slab_proxy)

            return web.json_response(status='ok')

        else:
            raise web.HTTPForbidden(reason='please authenticate at %s/auth' % (self.request.host))


class ScanDownload(web.View):

    async def get(self):

        site_id = self.request.match_info.get('site_id')
        slab_id = self.request.match_info.get('slab_id')
        scan_id = self.request.match_info.get('scan_id')

        session = await get_session(self.request)
        user = await get_user_info(self.request, session)

        if user:
            user_model = await user.models
            construction_model = user_model['construction']
            sites = await construction_model.sites
            target_site = sites[site_id]     # nsproxy
            # slab
            slabs_dict = await target_site.slabs
            scan_dict = await slabs_dict[slab_id].scans
            scan_proxy = scan_dict[scan_id]
            # scan_proxy # the cephproxyfile
            params = {'reqs':self.request,'scan_id':scan_id}
            await scan_proxy.read_and_handle(file_handler=file_handler(**params)) #the handler opens up a connection and does not close it until all the data are streamed

        else:
            raise web.HTTPForbidden(reason='please authenticate at %s/auth' % (self.request.host))

def file_handler(reqs, scan_id):

    async def handler(file):
        resp = web.StreamResponse(status=200,
                                  reason='OK',
                                  headers={'Content-Disposition': 'Attachment', 'filename': scan_id,
                                           "Content-Type": "application/octet-stream"})
        await resp.prepare(reqs)

        try:
            async for chunk in file.iter_chunked(64*1024):
                resp.write(chunk)
                await resp.drain()
            # Yield to the scheduler so other processes do stuff.
        except Exception as e:
            # So you can observe on disconnects and such.
            print(repr(e))
            raise
        finally:
            # await file.close() #for some reason close causes an aiohttp error, however the file closes itself by default when it times out.
            await resp.write_eof()
    return handler


async def authorize(app, handler):
    async def middleware(request):
        def need_auth_for_path(path):
            result = True
            for r in ['/auth', '/login', '/static/', '/signin', '/signout', '/_debugtoolbar/']:
                if path.startswith(r):
                    result = False
            return result
        # try:
        session = await get_session(request)
        token = session.get('token')
        if token:
            session_obj = await request.app.ow_app.authenticate_token(token)
            if session_obj:
                user = await session_obj.user
                if user:
                    return await handler(request)
                else:
                    print('not a valid user')
                    # Not a valid user.

        elif need_auth_for_path(request.path):
            print('no auth but requires it')
            raise web.HTTPUnauthorized(reason='please authenticate at %s/auth' % (request.host))

        else:
            return await handler(request)
        # except Exception as e:
        #     error = traceback.print_exc()
        #     print(error)
    return middleware




class OWRESTServer:
    def open(self):
        self.log = logging.getLogger('RESTServer')
        required_attrs = 'host', 'port', 'service'
        for attr in required_attrs:
            assert hasattr(self, attr), "Must specify {} in YAML config".format(attr)

    async def start(self, app):
        self.app = app
        await self.run_in_event_loop()

    async def run_in_event_loop(self):
        loop = asyncio.get_event_loop()

        # Genereate Secert Key

        fernet_key = fernet.Fernet.generate_key()

        secret_key = base64.urlsafe_b64decode(fernet_key)

        # Create the aioHttp Application
        self.aioapp = aioapp = web.Application(
            loop=loop,
            middlewares=[
                session_middleware(EncryptedCookieStorage(secret_key, max_age=600 )),
                # authorize
            ],
            debug=True
        )

        self.aioapp.ow_app = self.app

        # Setup Cors
        self.aio_cors = aiohttp_cors.setup(aioapp)

        # Setup the web server routes
        self.setup_routes()

        handler = aioapp.make_handler()
        server = loop.create_server(handler, self.host, self.port, ssl=None)
        await asyncio.gather(server, aioapp.startup(), loop=loop)
        self.log.info('Listening on {}:{}'.format(self.host, self.port))

    def setup_routes(self):
        # add end points here.
        self.aioapp.router.add_route('POST', '/v1/auth/', auth_handler)
        self.aioapp.router.add_route('GET', '/v1/ping/', ping)
        self.aioapp.router.add_route('GET', '/v1/user/', User)
        self.aioapp.router.add_route('*', '/v1/sites/', Sites)
        self.aioapp.router.add_route('*', '/v1/sites/{site_id}/', Site) #view the site info
        self.aioapp.router.add_route('*', '/v1/sites/{site_id}/slabs/', Slabs) #view slabs in the site
        self.aioapp.router.add_route('*', '/v1/sites/{site_id}/{slab_id}/', Slab) # view slab info or upload scans
        self.aioapp.router.add_route('*', '/v1/sites/{site_id}/{slab_id}/scans', Scans) #view list of scans
        self.aioapp.router.add_route('*', '/v1/sites/{site_id}/{slab_id}/{scan_id}', Scan) #dl scan or replace original scan with new scan
        self.aioapp.router.add_route('*', '/v1/sites/{site_id}/{slab_id}/{scan_id}/download', ScanDownload) #dl scan or replace original scan with new scan

