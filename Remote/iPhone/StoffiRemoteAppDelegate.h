//
//  StoffiRemoteAppDelegate.h
//  StoffiRemote
//
//  Created by Fredrik Gadnell on 9/11/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <UIKit/UIKit.h>
#import "RESTRequest.h"

@interface StoffiRemoteAppDelegate : NSObject <UIApplicationDelegate, RestRequestDelegate> {
    UINavigationController *nav;
}

@property (nonatomic, retain) IBOutlet UIWindow *window;

@end
