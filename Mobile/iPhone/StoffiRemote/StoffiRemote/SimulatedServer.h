//
//  SimulatedServer.h
//  StoffiRemote
//
//  Created by Fredrik Gadnell on 10/1/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "Configuration.h"


@interface SimulatedServer : NSObject {
    NSDictionary *configuration;
}

@property (retain) NSDictionary *configuration;

+ (SimulatedServer *)sharedServer;
- (NSDictionary *)testConfiguration;

@end
