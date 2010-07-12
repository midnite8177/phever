from google.appengine.ext import webapp
from google.appengine.ext.webapp import template

class TestPage(webapp.RequestHandler):
    def get(self):
        ifile = open('result1.log')        
        """3    Michel Laframboise    Quebec, Canada.    52.9399159    -73.5491361    ../swtgallery/Nature/RedrobinL.jpg"""
        result = []
        
        for iline in ifile:
            iid, name, location, lat, lng, imgurl, title, content = map(lambda i: i.strip(), iline.split('\t'))
            imgurl = imgurl.replace("../", "http://www.skywatchertelescope.net/")
            thumburl = imgurl.replace("L.", "S.")
            
            if len(content) > 40:
                contents = map(lambda i: i.strip(), content.split(' '))
                new_content = []
                index = 0
                bound = 40
                
                for i in contents:
                    if index + len(i) > bound:
                        new_content.append("<br />")
                        bound += 40                        
                    
                    new_content.append(i)
                    index += len(i)                                                            
                
                content = " ".join(new_content)                        
                                
            result.append((iid,name,location,lat,lng,imgurl, thumburl, title, content))
            
        self.response.out.write(template.render("template/TestPage.html", {'result': result}))