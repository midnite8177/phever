from google.appengine.api import users
from google.appengine.ext import webapp
from google.appengine.ext.webapp.util import run_wsgi_app
from google.appengine.ext.db import Key
from controls.ajaxs import *
from db.models import UserData
import random

class MainPage(webapp.RequestHandler):
        
    def get(self):        
        user = users.get_current_user()

        if user:
            userdata = UserData.all().filter("email = ", user.email()).get()
            if not userdata:
                userdata = UserData(email = user.email(), secret = str(abs(hash(random.random())))[:5])
                userdata.put()
            
            self.response.out.write(userdata.secret)
                            
        else:
            self.redirect(users.create_login_url(self.request.uri))


application = webapp.WSGIApplication([
                                      ('/', MainPage),
                                      ('/UploadImage/', UploadImage),
                                      ('/QueryImage/', QueryImage),
                                      ('/QueryUserImage/', QueryUserImage),
                                      ], debug=True)


def main():
    run_wsgi_app(application)

def profile_main():
    import cProfile, pstats
    prof = cProfile.Profile()
    prof = prof.runctx( "real_main()", globals(), locals() )    
#    print '<pre>'
    stats = pstats.Stats( prof )
    stats.sort_stats( "cumulative" )
    stats.print_stats( 200 )
    stats.print_callees()
    stats.print_callers()
#    print '</pre>'


if __name__ == "__main__":
    main()
