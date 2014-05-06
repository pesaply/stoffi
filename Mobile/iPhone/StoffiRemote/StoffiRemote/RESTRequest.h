//
//  RestRequest.h
//  StoffiRemote
//
//  Created by Fredrik Gadnell on 9/18/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>

@class RESTRequest;

@protocol RestRequestDelegate <NSObject>
@required
- (void)restRequest:(RESTRequest *)request didLoadResult:(id)jsonObject;
- (void)restRequestDidFail:(RESTRequest *)request;
@end

#define kRequestTypeNone -1

@interface RESTRequest : NSObject {
    id<RestRequestDelegate> delegate;
    
    int requestType;
    NSURLConnection *connection;
    NSMutableData *receivedData;
}

@property (readwrite) int requestType;
@property (assign) id<RestRequestDelegate> delegate;
@property (assign) NSURLConnection *connection;
@property (readwrite) BOOL shouldLog;

+ (RESTRequest *)restRequestWithURL:(NSString *)url method:(NSString *)httpMethod delegate:(id<RestRequestDelegate>)delegate;

@end

