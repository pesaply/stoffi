//
//  RESTClientSimulator.m
//  StoffiRemote
//
//  Created by Fredrik Gadnell on 9/29/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "RESTClientSimulator.h"
#import "SimulatedServer.h"
#import "User.h"

#define kKeyRequest @"request"
#define kKeyDelegate @"delegate"
#define kKeyResult @"result"

@implementation RESTClientSimulator

+ (RESTClientSimulator *)sharedClientSimulator
{
    static dispatch_once_t once;
    static RESTClientSimulator *sharedClient;
    dispatch_once(&once, ^ {sharedClient = [[RESTClientSimulator alloc] init];});
    return sharedClient;
}

// Performs a simulated callback that tries to immitate the servers response.
- (RESTRequest *)simulatedRequestWithPath:(NSString *)path httpMethod:(NSString *)method delegate:(id<RestRequestDelegate>)delegate {
    RESTRequest *request = nil;
    
    if ([method isEqualToString:@"GET"]) {
        
        if ([path isEqualToString:@"/configuration.json"]) {
            // Package callback
            request = [[[RESTRequest alloc] init] autorelease];
            id result = [[SimulatedServer sharedServer] configuration];
            NSDictionary *dict = [NSDictionary dictionaryWithObjectsAndKeys:
                                  request, kKeyRequest, 
                                  delegate, kKeyDelegate,
                                  result, kKeyResult,
                                  nil];
            
            [self performSelector:@selector(simulatedCallback:) withObject:dict afterDelay:1.0];
        }
    }
    
    if ([method isEqualToString:@"POST"]) {
        
        if ([path isEqualToString:@"/configuration.json"]) {
            // Package callback
            request = [[[RESTRequest alloc] init] autorelease];
            NSDictionary *result = [NSDictionary dictionaryWithObjectsAndKeys:@"ok", @"status", nil];
            NSDictionary *dict = [NSDictionary dictionaryWithObjectsAndKeys:
                                  request, kKeyRequest, 
                                  delegate, kKeyDelegate,
                                  result, kKeyResult,
                                  nil];
            
            // Update configuration on server
            NSDictionary *config = [NSDictionary dictionaryWithDictionary:[User currentUser].configuration.configurationDictionary];
            [[SimulatedServer sharedServer] performSelector:@selector(setConfiguration:) withObject:config afterDelay:0.5];
            
            [self performSelector:@selector(simulatedCallback:) withObject:dict afterDelay:1.0];
        }
        
    }
    
    return request;
}

// Performs a simulated callback on the delegate.
- (void)simulatedCallback:(NSDictionary *)dict {
    RESTRequest *request = [dict objectForKey:kKeyRequest];
    id<RestRequestDelegate> delegate = [dict objectForKey:kKeyDelegate];
    id result = [dict objectForKey:kKeyResult];
    
    [delegate restRequest:request didLoadResult:result];
}

- (NSDictionary *)testConfiguration {
    return [NSDictionary dictionaryWithObjectsAndKeys:
            @"paused", @"MediaState",
            nil];
}

@end
