//
//  OAuthTestViewController.h
//  StoffiRemote
//
//  Created by Fredrik Gadnell on 9/19/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <UIKit/UIKit.h>
#import "StoffiOauth.h"
#import "RESTClient.h"


@interface OAuthTestViewController : UIViewController<StoffiOAuthManagerDelegate, RestRequestDelegate> {
    
}

@end
