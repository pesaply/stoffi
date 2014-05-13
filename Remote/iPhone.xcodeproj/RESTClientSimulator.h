//
//  RESTClientSimulator.h
//  StoffiRemote
//
//  Created by Fredrik Gadnell on 9/29/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "RESTClient.h"


@interface RESTClientSimulator : RESTClient {
    
}

+ (RESTClientSimulator *)sharedClientSimulator;
- (RESTRequest *)simulatedRequestWithPath:(NSString *)path httpMethod:(NSString *)method delegate:(id<RestRequestDelegate>)delegate;
- (NSDictionary *)testConfiguration;
- (void)simulatedCallback:(NSDictionary *)dict;

@end
