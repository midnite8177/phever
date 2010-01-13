from google.appengine.ext import db, webapp
from db.models import Image, UserImage, UserData
from google.appengine.api import users

# hash # tags
class UploadImage(webapp.RequestHandler):
    def get(self):
        return self.post()
    
    def post(self):        
        datas = self.request.get_all("data")
        secret = self.request.get("secret")
                    
        userdata = UserData.all().filter("secret = ", secret).get()
        if userdata:
            email = userdata.email
        else:
            return        
        
        # merge
        imagedatas = {}
        for data in datas:
            hash, tags = data.split(":")
            if hash in imagedatas:
                imagedatas[hash] += "," + tags
            else:
                imagedatas[hash] = tags        
        
        imagedatas = imagedatas.items()
        keys = [db.Key.from_path("Image", "hash:" + hash) for hash, _ in imagedatas]
        userkeys = [db.Key.from_path("UserImage", "email:" + email + "#hash:" + hash) for hash, _ in imagedatas]
                
        images = Image.get(keys)
        userimages = UserImage.get(userkeys)

        updated = []
        for imagedata, image, userimage in zip(imagedatas, images, userimages):
            hash, tags = imagedata
            tags = [kk for kk in [k.strip() for k in tags.split(",")] if kk]
            
            if not image:
                image = Image(key_name = "hash:" + hash)
                image.tags = tags
                updated.append(image)
            elif not set(image.tags) == set(tags):
                image.tags = list(set(image.tags).union(tags))
                updated.append(image)
                
            if not userimage:
                userimage = UserImage(key_name = "email:" + email + "#hash:" + hash, email = email)
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
        hashs = self.request.get_all("hash")                
                
        keys = [db.Key.from_path("Image", "hash:" + hash) for hash in hashs]    
                
        images = Image.get(keys)        

        returned = []
        for image in images:
            
            if image:
                returned.append(image.tags)
            else:
                returned.append([])
                
        print returned
        
        
class QueryUserImage(webapp.RequestHandler):
    def get(self):
        return self.post()
    
    def post(self):
        hashs = self.request.get_all("hash")
        secret = self.request.get("secret")
             
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
                returned.append(image.tags)
            else:
                returned.append([])
                
        print returned
        