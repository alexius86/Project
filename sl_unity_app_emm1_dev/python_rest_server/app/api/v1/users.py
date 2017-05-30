import re
import falcon
import json

from zmq import asyncio

from solutions.construction.rest_api.app import log
from solutions.construction.rest_api.app.api.common.base import BaseResource
from solutions.construction.rest_api.app.utils.hooks import auth_required
from solutions.construction.rest_api.app.utils.auth import encrypt_token, hash_password, verify_password, uuid
# from app.model.users import User

from solutions.construction.rest_api.app.errors import AppError, InvalidParameterError, UserNotExistsError, PasswordNotMatch

LOG = log.get_logger()


FIELDS = {
    'username': {
        'type': 'string',
        'required': True,
        'minlength': 4,
        'maxlength': 20
    },
    'email': {
        'type': 'string',
        'regex': '[a-zA-Z0-9._-]+@(?:[a-zA-Z0-9-]+\.)+[a-zA-Z]{2,4}',
        'required': True,
        'maxlength': 320
    },
    'password': {
        'type': 'string',
        'regex': '[0-9a-zA-Z]\w{3,14}',
        'required': True,
        'minlength': 8,
        'maxlength': 64
    },
    'info': {
        'type': 'dict',
        'required': False
    }
}



class Collection(BaseResource):
    """
    Handle for endpoint: /v1/users
    """
    def on_post(self, req, res):
        session = req.context['session']
        user_req = req.context['data']
        if user_req:
            user = User()
            user.username = user_req['username']
            user.email = user_req['email']
            user.password = hash_password(user_req['password']).decode('utf-8')
            user.info = user_req['info'] if 'info' in user_req else None
            sid = uuid()
            user.sid = sid
            user.token = encrypt_token(sid).decode('utf-8')
            session.add(user)
            self.on_success(res, None)
        else:
            raise InvalidParameterError(req.context['data'])

    @falcon.before(auth_required)
    def on_get(self, req, res):
        session = req.context['session']
        # user_dbs = session.query(User).all()
        # if user_dbs:
        #     obj = [user.to_dict() for user in user_dbs]
        #     self.on_success(res, obj)
        # else:
        #     raise AppError()

    @falcon.before(auth_required)
    def on_put(self, req, res):
        pass


class Item(BaseResource):
    """
    Handle for endpoint: /v1/users/{user_id}
    """
    @falcon.before(auth_required)
    def on_get(self, req, res, user_id):
        session = req.context['session']
        pass


class Self(BaseResource):
    """
    Handle for endpoint: /v1/users/self
    """
    LOGIN = 'login'
    RESETPW = 'resetpw'

    def on_get(self, req, res):
        cmd = re.split('\\W+', req.path)[-1:][0]
        if cmd == Self.LOGIN:
            self.process_login(req, res)
        elif cmd == Self.RESETPW:
            self.process_resetpw(req, res)

    def process_login(self, req, res):
        email = req.params['email']
        password = req.params['password']
        session = req.context['session']
        try:
            user_db = User.find_by_email(session, email)
            if verify_password(password, user_db.password.encode('utf-8')):
                self.on_success(res, user_db.to_dict())
            else:
                raise PasswordNotMatch()

        except NoResultFound:
            raise UserNotExistsError('User email: %s' % email)

    @falcon.before(auth_required)
    def process_resetpw(self, req, res):
        pass


class Ping(BaseResource):
    def on_get(self, req, resp):
        result = dict(status='ok')
        resp.body = json.dumps(result)

class Sites(BaseResource):
    def create_site(self, data):
        pass

    def on_get(self, req, resp):
        sites_pre_proxy = asyncio.ensure_future(self.ow_app.get(OWSites.__name__))
        sites_proxy_obj = self.loop.run_until_complete(sites_pre_proxy)
        pass


    def on_post(self, req, resp):
        # session = req.context['session']
        user_req = req.get_param('name')

        user_req = req.context['data']
        if user_req:
            # self.create_site(user_req)
            site = asyncio.ensure_future(
                self.ow_app.create(
                    OWSites.__name__,
                    name = user_req.get('name'),
                    description = user_req.get('description')
                )
            )

            # get the proxy object and wait for it to complete
            site_proxy_obj = self.loop.run_until_complete(site)


            # Update the model
            asyncio.ensure_future(self.ow_app.update(site_proxy_obj))


            self.on_success(resp, None)
        else:
            raise InvalidParameterError(req.context['data'])