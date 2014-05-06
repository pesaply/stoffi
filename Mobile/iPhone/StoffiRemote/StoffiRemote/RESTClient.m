//
//  RESTClient.m
//  StoffiRemote
//
//  Created by Fredrik Gadnell on 9/18/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "RESTClient.h"
#import "RESTClientSimulator.h"


@implementation RESTClient

@synthesize baseURL, shouldSimulateResponse;

- (id)init {
    self = [super init];
    
    if (self) {
        baseURL = @"";
        shouldSimulateResponse = NO;
    }
    return self;
}

+ (RESTClient *)sharedClient
{
    static dispatch_once_t once;
    static RESTClient *sharedClient;
    dispatch_once(&once, ^ {sharedClient = [[RESTClient alloc] init];});
    return sharedClient;
}

- (RESTRequest *)get:(NSString *)path delegate:(id<RestRequestDelegate>)delegate {
    
    return [self requestWithPath:path httpMethod:@"GET" delegate:delegate];
}

- (RESTRequest *)post:(NSString *)path delegate:(id<RestRequestDelegate>)delegate {
    return [self requestWithPath:path httpMethod:@"POST" delegate:delegate];
}

- (RESTRequest *)put:(NSString *)path delegate:(id<RestRequestDelegate>)delegate {
    return [self requestWithPath:path httpMethod:@"PUT" delegate:delegate];
}

- (RESTRequest *)del:(NSString *)path delegate:(id<RestRequestDelegate>)delegate {
    return [self requestWithPath:path httpMethod:@"DELETE" delegate:delegate];
}

- (RESTRequest *)requestWithPath:(NSString *)path httpMethod:(NSString *)method delegate:(id<RestRequestDelegate>)delegate {
    // Simulated request
    if (shouldSimulateResponse) {
        return [[RESTClientSimulator sharedClientSimulator] simulatedRequestWithPath:path httpMethod:method delegate:delegate];
    }
    
    // Normal request
    NSString *url = [baseURL stringByAppendingString:path];
    return [RESTRequest restRequestWithURL:url method:method delegate:delegate];
}

@end
