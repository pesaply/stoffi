//
//  User.h
//  StoffiRemote
//
//  Created by Fredrik Gadnell on 9/29/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "GaddREST.h"
#import "Configuration.h"


#define kUserIDUnknown -1

@interface User : NSObject<RestRequestDelegate> {
    int userID;
    Configuration *configuration;
}

@property (readonly) int userID;
@property (retain) Configuration *configuration;

+ (User *)newUser;
+ (User *)currentUser;

- (void)pullConfiguration;
- (void)pushConfiguration;

@end
