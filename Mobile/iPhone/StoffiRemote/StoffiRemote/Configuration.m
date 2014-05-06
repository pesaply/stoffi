//
//  Configuration.m
//  StoffiRemote
//
//  Created by Fredrik Gadnell on 9/30/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "Configuration.h"


@implementation Configuration

@synthesize configurationDictionary;

+ (Configuration *)configurationWithDictionary:(NSDictionary *)dictionary {
    Configuration *_ = [[Configuration alloc] init];
    
    _.configurationDictionary = [NSMutableDictionary dictionaryWithDictionary:dictionary];
    
    return [_ autorelease];
}

- (NSString *)description {
    return [configurationDictionary description];
}

- (NSNumber *)property:(Property)property {
    NSAssert(property >= 0 && property < NumberOfProperties, @"Unknown property");
    
    return [configurationDictionary objectForKey:[self keyForProperty:property]];
}

- (void)setProperty:(Property)property toValue:(NSNumber *)number {
    NSAssert(property >= 0 && property < NumberOfProperties, @"Unknown property");
    
    [configurationDictionary setObject:[self stringForValue:number 
                                                 ofProperty:property]
                                forKey:[self keyForProperty:property]];
}

- (NSString *)keyForProperty:(Property)property {
    return (property == PropertyMediaState ? @"MediaState" : 
            property == PropertyVolume ? @"volume" :
            
            @"");
}

- (NSString *)stringForValue:(NSNumber *)value ofProperty: (Property)property  {
    return (property == PropertyMediaState && [value intValue] == MediaStatePlaying ? @"playing" : 
            property == PropertyMediaState && [value intValue] == MediaStatePaused ? @"paused" :
            property == PropertyMediaState && [value intValue] == MediaStateStopped ? @"stopped" :
            
            property == PropertyVolume ? [NSString stringWithFormat:@"%f", [value doubleValue]] :
            
            @"");
}

@end