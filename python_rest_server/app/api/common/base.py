import falcon
import json

try:
    from collections import OrderedDict
except ImportError:
    OrderedDict = dict

from solutions.construction.rest_api.app import log
# from solutions.construction.rest_api.app.utils.alchemy import new_alchemy_encoder
from solutions.construction.rest_api.app.config import BRAND_NAME, POSTGRES
from solutions.construction.rest_api.app.errors import NotSupportedError
import zmq.asyncio
import asyncio
import mlcortex
from bootloader import Bootloader, yaml_to_config

# Now we are adding mlcortex into the path to be able to call workflow items
path = mlcortex.__path__[0]  # getting the full(absolute)path of the mlcortex, since CONFIG for yaml is located there
loop = zmq.asyncio.ZMQEventLoop()
asyncio.set_event_loop(loop)
mlcortex_path = '%s/CONFIG/application_server/AS_overwatch_rest_api.yaml' % path
bootloader = Bootloader(yaml_to_config(mlcortex_path))
ow_app = loop.run_until_complete(bootloader.startup())

LOG = log.get_logger()


class BaseResource(object):
    HELLO_WORLD = {
        'server': '%s' % BRAND_NAME,
        'database': 'some cool database'
    }
    ow_app = ow_app
    loop = loop

    def to_json(self, body_dict):
        return json.dumps(body_dict)

    def from_db_to_json(self, db):
        pass
        # return json.dumps(db, cls=new_alchemy_encoder())

    def on_error(self, res, error=None):
        res.status = error['status']
        meta = OrderedDict()
        meta['code'] = error['code']
        meta['message'] = error['message']

        obj = OrderedDict()
        obj['meta'] = meta
        res.body = self.to_json(obj)

    def on_success(self, res, data=None):
        res.status = falcon.HTTP_200
        meta = OrderedDict()
        meta['code'] = 200
        meta['message'] = 'OK'

        obj = OrderedDict()
        obj['meta'] = meta
        obj['data'] = data
        res.body = self.to_json(obj)

    def on_get(self, req, res):
        if req.path == '/':
            res.status = falcon.HTTP_200
            res.body = self.to_json(self.HELLO_WORLD)
        else:
            raise NotSupportedError(method='GET', url=req.path)

    def on_post(self, req, res):
        raise NotSupportedError(method='POST', url=req.path)

    def on_put(self, req, res):
        raise NotSupportedError(method='PUT', url=req.path)

    def on_delete(self, req, res):
        raise NotSupportedError(method='DELETE', url=req.path)
