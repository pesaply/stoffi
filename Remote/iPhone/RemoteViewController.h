//
//  RemoteViewController.h
//  StoffiRemote
//
//  Created by Fredrik Gadnell on 9/26/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <UIKit/UIKit.h>


@interface RemoteViewController : UIViewController {
    IBOutlet UIButton *playButton;
    IBOutlet UIButton *pauseButton;
}

- (void)presentLoginScreenAnimated:(BOOL)animated;
- (IBAction)logoutPressed;
- (IBAction)playPressed;
- (IBAction)pausePressed;

@end
