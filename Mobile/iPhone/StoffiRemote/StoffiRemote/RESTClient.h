//
//  RESTClient.h
//  StoffiRemote
//
//  Created by Fredrik Gadnell on 9/18/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "RESTRequest.h"


@interface RESTClient : NSObject {
    NSString *baseURL;
    BOOL shouldSimulateResponse;
}

@property (retain) NSString *baseURL;
@property (readwrite) BOOL shouldSimulateResponse;

+ (RESTClient *)sharedClient;
- (RESTRequest *)get:(NSString *)path delegate:(id<RestRequestDelegate>)delegate;
- (RESTRequest *)post:(NSString *)path delegate:(id<RestRequestDelegate>)delegate;
- (RESTRequest *)put:(NSString *)path delegate:(id<RestRequestDelegate>)delegate;
- (RESTRequest *)del:(NSString *)path delegate:(id<RestRequestDelegate>)delegate;
- (RESTRequest *)requestWithPath:(NSString *)path httpMethod:(NSString *)method delegate:(id<RestRequestDelegate>)delegate;

@end