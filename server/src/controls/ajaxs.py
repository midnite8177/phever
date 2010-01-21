from google.appengine.ext import db, webapp
from db.models import Image, UserImage, UserData
from google.appengine.api import users
import logging


# datas = hash1: tag1;tag2;tag3;tag4, hash2:tag1;tag2;tag3
class UploadImage(webapp.RequestHandler):
    def get(self):
        return self.post()
    
    def post(self):        
        datas = self.request.get("data")
        secret = self.request.get("secret")        
            
        datas = datas.split(',')        
            
        userdata = UserData.all().filter("secret = ", secret).get()
        if userdata:
            email = userdata.email.lower()
        else:
            return        
        
        # merge
        imagedatas = {}
        for data in datas:
            hash, tags = data.split(":")
            if hash in imagedatas:
                imagedatas[hash] += ";" + tags
            else:
                imagedatas[hash] = tags        
        
        hkeys = imagedatas.keys()
        
        keys = [db.Key.from_path("Image", "hash:" + hash) for hash in hkeys]
        userkeys = [db.Key.from_path("UserImage", "email:" + email + "#hash:" + hash) for hash in hkeys]
                
        images = Image.get(keys)
        userimages = UserImage.get(userkeys)

        updated = []
        for hash, image, userimage in zip(hkeys, images, userimages):
            tags = imagedatas[hash]
            tags = [kk for kk in [k.strip() for k in tags.split(";")] if kk]
            
            if not image:
                image = Image(key_name = "hash:" + hash, hash = hash)
                image.tags = tags
                updated.append(image)
            elif not set(image.tags) == set(tags):
                image.tags = list(set(image.tags).union(tags))
                updated.append(image)
                
            if not userimage:
                userimage = UserImage(key_name = "email:" + email + "#hash:" + hash, email = email, hash = hash)
                userimage.tags = tags                
                updated.append(userimage)
            elif not set(userimage.tags) == set(tags):
                userimage.tags = list(set(image.tags).union(tags))
                updated.append(userimage)
                
        db.put(updated)            
        return
    
class QueryImage(webapp.RequestHandler):
    def get(self):
        return self.post()
    
    def post(self):        
        hashs = self.request.get("hash")
        hashs = hashs.split(',')                
                
        keys = [db.Key.from_path("Image", "hash:" + hash) for hash in hashs]    
                
        images = Image.get(keys)        

        returned = []
        for image in images:        
            if image:
                returned.append(";".join(image.tags))
            else:
                returned.append("")
                
        self.response.out.write(','.join(returned))
                
class QueryUserImage(webapp.RequestHandler):
    def get(self):
        return self.post()
    
    def post(self):
        hashs = self.request.get("hash")
        secret = self.request.get("secret")

        hashs = hashs.split(',')
             
        userdata = UserData.all().filter("secret = ", secret).get()
        if userdata:
            email = userdata.email
        else:
            return   
        
        userkeys = [db.Key.from_path("UserImage", "email:" + email + "#hash:" + hash) for hash in hashs]
        userimages = UserImage.get(userkeys)
        
        returned = []
        for image in userimages:            
            if image:
                returned.append(";".join(image.tags))
            else:
                returned.append("")
                
        self.response.out.write(','.join(returned))
        