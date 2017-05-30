from marshmallow_sqlalchemy import ModelSchema
from sqlalchemy import Column
from sqlalchemy import String, Integer, LargeBinary
from sqlalchemy.dialects.postgresql import JSONB

from app.model.base import Base
from app.config import UUID_LEN
from app.utils import alchemy


class Sites(Base):
    site_id = Column(Integer, primary_key=True)
    site_name= Column(String(100), nullable=False)
    site_description = Column(String(320), nullable=False)

    def __repr__(self):
        return "<Site(name='%s', description='%s')>" % \
               (self.site_name, self.site_description)

    @classmethod
    def get_id(cls):
        return Sites.user_id

    @classmethod
    def find_by_name(cls, session, name):
        return session.query(Sites).filter(Sites.site_name == name).one()


class SitesSchema(ModelSchema):
    class Meta:
        model = Sites

sites_schema = SitesSchema(many=True)
site_schema = SitesSchema()