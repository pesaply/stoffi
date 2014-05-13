//
//  JSONRequest.m
//  StoffiRemote
//
//  Created by Fredrik Gadnell on 9/18/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "RESTRequest.h"
#import "SBJSON.h"
#import "StoffiOAuthManager.h"

@implementation RESTRequest

@synthesize delegate, connection, shouldLog, requestType;

- (id)init {
    self = [super init];
    
    if (self) {
        receivedData = [[NSMutableData alloc] init];
        shouldLog = NO;
        requestType = -1;
    }
    return self;
}

+ (RESTRequest *)restRequestWithURL:(NSString *)urlString method:(NSString *)httpMethod delegate:(id<RestRequestDelegate>)del {
    RESTRequest *_ = [[RESTRequest alloc] init];
    
    _.delegate = del;
    
     urlString = [urlString stringByReplacingPercentEscapesUsingEncoding:NSISOLatin1StringEncoding];
    
    NSMutableURLRequest *request = [NSMutableURLRequest requestWithURL:[NSURL URLWithString:urlString]
                                             cachePolicy:NSURLRequestUseProtocolCachePolicy
                                         timeoutInterval:15.0];
    [request setHTTPMethod:httpMethod];
    
    // Sign with stoffi OAuth token
    BOOL didSign = [[StoffiOAuthManager sharedManager] signRequest:request];
    if (!didSign) {
        NSLog(@"RESTRequest: Could not sign request with token. Perhaps the token has expired? Aborting request...");
        return nil;
    }
    
    _.connection = [NSURLConnection connectionWithRequest:request delegate:_];
    
    NSLog(@"RESTRequest: Will open connection to %@", urlString);
    
    if (!_.connection) {
        NSLog(@"RESTRequest failed to create NSURLConnection object");
        return nil;
    }
    
    return _;
}

- (void)connection:(NSURLConnection *)connection didReceiveResponse:(NSURLResponse *)response
{
    if (!receivedData)
        return;
    
    if (shouldLog) NSLog(@"RESTRequest: Did receive response");
    
    [receivedData setLength:0];
}

- (void)connection:(NSURLConnection *)connection didReceiveData:(NSData *)data
{
    if (!receivedData)
        return;
    
    if (shouldLog) NSLog(@"RESTRequest: Did receive data");
    
    // Append received data
    [receivedData appendData:data];
}

- (void)connection:(NSURLConnection *)connection
  didFailWithError:(NSError *)error
{
    if (shouldLog) NSLog(@"RESTRequest did fail with error: %@", [error localizedDescription]);
    [delegate restRequestDidFail:self];
}

- (void)connectionDidFinishLoading:(NSURLConnection *)conn
{
    if (!receivedData)
        return;
    
    if (shouldLog) NSLog(@"RESTRequest: Did finish downloading");
    
    NSString *receivedString = [[[NSString alloc] initWithData:receivedData encoding:NSUTF8StringEncoding] autorelease];
    
    // Parse returned data
    SBJSON *jsonParser = [[SBJSON new] autorelease];
    NSError *error = nil;
    NSDictionary *receivedDict = (NSDictionary *)[jsonParser objectWithString:receivedString error:&error];
    if (error) {
        if (shouldLog) NSLog(@"RESTRequest: Error parsing returned data (%@): %@", [error localizedDescription], receivedString);
        [delegate restRequestDidFail:self];
    } else {
        if (receivedDict)
            [delegate restRequest:self didLoadResult:receivedDict];
        else {
            if (shouldLog) NSLog(@"RESTRequest: jsonParser could not parse returned data");
            [delegate restRequestDidFail:self];
        }
    }
}

- (void)dealloc {
    [receivedData release];
    
    [super dealloc];
}

@end
