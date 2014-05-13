//
//  LoginViewController.h
//  StoffiRemote
//
//  Created by Fredrik Gadnell on 9/26/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <UIKit/UIKit.h>
#import "StoffiOauth.h"
#import "RESTClient.h"

@interface LoginViewController : UIViewController<StoffiOAuthManagerDelegate> {
    
}

- (IBAction)loginPressed;

@end
