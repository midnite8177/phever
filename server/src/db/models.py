from google.appengine.api.users import User
from google.appengine.ext import db

class UserData(db.Model):
    email = db.EmailProperty(required = True)
    secret = db.StringProperty( required = True)
    
class Image(db.Model):
    hash = db.StringProperty()    
    tags = db.StringListProperty()
    
class UserImage(db.Model):
    email = db.EmailProperty(required = True)
    hash = db.StringProperty()
    tags = db.StringListProperty()