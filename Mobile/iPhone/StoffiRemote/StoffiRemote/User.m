//
//  User.m
//  StoffiRemote
//
//  Created by Fredrik Gadnell on 9/29/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "User.h"

typedef enum  {
    UserRequestTypePullConfiguration,
    UserRequestTypePushConfiguration
} UserRequestType;

@implementation User

@synthesize userID, configuration;

- (id)init {
    self = [super init];
    
    if (self) {
        userID = kUserIDUnknown;
        configuration = nil;
    }
    return self;
}

+ (User *)newUser {
    User *_ = [[User alloc] init];
    
    return [_ autorelease];
}

+ (User *)currentUser {
    static dispatch_once_t once;
    static User *currentUser;
    dispatch_once(&once, ^ {currentUser = [[User alloc] init];});
    return currentUser;
}

- (void)pullConfiguration {
    RESTRequest *request = [[RESTClient sharedClient] get:@"/configuration.json" delegate:self];
    request.requestType = UserRequestTypePullConfiguration;
}

- (void)pushConfiguration {
    RESTRequest *request = [[RESTClient sharedClient] post:@"/configuration.json" delegate:self];
    request.requestType = UserRequestTypePushConfiguration;
}

#pragma mark - RestRequestDelegate methods

- (void)restRequest:(RESTRequest *)request didLoadResult:(id)jsonObject {
    if (request.requestType == UserRequestTypePullConfiguration) {
        self.configuration = [Configuration configurationWithDictionary:jsonObject];
        NSLog(@"Current user successfully pulled configuration from server: %@", self.configuration);
    }
    
    if (request.requestType == UserRequestTypePushConfiguration) {
        NSLog(@"Current user successfully pushed configuration to server: %@", self.configuration);
    }
}

- (void)restRequestDidFail:(RESTRequest *)request {
    
}

@end
