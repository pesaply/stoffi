//
//  Configuration.h
//  StoffiRemote
//
//  Created by Fredrik Gadnell on 9/30/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>

typedef enum {
    PropertyMediaState,
    PropertyVolume,
    
    // NumberOfProperties will always be correct, from the properties of enums
    NumberOfProperties
} Property;

typedef enum {
    MediaStatePaused,
    MediaStatePlaying,
    MediaStateStopped
} MediaState;

@interface Configuration : NSObject {
    NSMutableDictionary *configurationDictionary;
}

@property (retain) NSMutableDictionary *configurationDictionary;

+ (Configuration *)configurationWithDictionary:(NSDictionary *)dictionary;
- (NSNumber *)property:(Property)property;
- (void)setProperty:(Property)property toValue:(NSNumber *)number;
- (NSString *)keyForProperty:(Property)property;
- (NSString *)stringForValue:(NSNumber *)value ofProperty: (Property)property;

@end