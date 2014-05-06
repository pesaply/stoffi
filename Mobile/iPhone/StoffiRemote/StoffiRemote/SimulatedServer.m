//
//  SimulatedServer.m
//  StoffiRemote
//
//  Created by Fredrik Gadnell on 10/1/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "SimulatedServer.h"


@implementation SimulatedServer

@synthesize configuration;

- (id)init {
    self = [super init];
    
    if (self) {
        configuration = nil;
    }
    return self;
}

+ (SimulatedServer *)sharedServer
{
    static dispatch_once_t once;
    static SimulatedServer *sharedServer;
    dispatch_once(&once, ^ {sharedServer = [[SimulatedServer alloc] init];});
    return sharedServer;
}

- (NSDictionary *)configuration {
    if (!configuration)
        self.configuration = [self testConfiguration];
    
    return configuration;
}

- (void)setConfiguration:(NSDictionary *)d {
    if (configuration) 
        configuration = nil;
    
    configuration = [d retain];
}
         
- (NSDictionary *)testConfiguration {
    return [NSDictionary dictionaryWithObjectsAndKeys:
            @"paused", @"MediaState",
            nil];
}

@end