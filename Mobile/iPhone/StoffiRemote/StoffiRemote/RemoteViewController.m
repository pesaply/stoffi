//
//  RemoteViewController.m
//  StoffiRemote
//
//  Created by Fredrik Gadnell on 9/26/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "RemoteViewController.h"
#import "LoginViewController.h"
#import "User.h"

@implementation RemoteViewController

- (id)initWithNibName:(NSString *)nibNameOrNil bundle:(NSBundle *)nibBundleOrNil
{
    self = [super initWithNibName:nibNameOrNil bundle:nibBundleOrNil];
    if (self) {
        // Custom initialization
    }
    return self;
}

- (IBAction)logoutPressed {
    User *user = [User currentUser];
    user = nil;
    
    [self presentLoginScreenAnimated:YES];
}

- (void)presentLoginScreenAnimated:(BOOL)animated {
    LoginViewController *lvc = [[LoginViewController alloc] init];
    lvc.modalTransitionStyle = UIModalTransitionStyleFlipHorizontal;
    [self presentModalViewController:lvc animated:animated];
}

- (void)dealloc
{
    [super dealloc];
}

- (void)didReceiveMemoryWarning
{
    // Releases the view if it doesn't have a superview.
    [super didReceiveMemoryWarning];
    
    // Release any cached data, images, etc that aren't in use.
}

#pragma mark - Playback control

- (void)playPressed {
    playButton.hidden = YES;
    pauseButton.hidden = NO;
    
    User *user = [User currentUser];
    [user.configuration setProperty:PropertyMediaState toValue:[NSNumber numberWithInt:MediaStatePlaying]];
    [user pushConfiguration];
}

- (void)pausePressed {
    playButton.hidden = NO;
    pauseButton.hidden = YES;
    
    User *user = [User currentUser];
    [user.configuration setProperty:PropertyMediaState toValue:[NSNumber numberWithInt:MediaStatePaused]];
    [user pushConfiguration];
}

#pragma mark - View lifecycle

- (void)viewDidLoad
{
    [super viewDidLoad];
    // Do any additional setup after loading the view from its nib.
}

- (void)viewDidUnload
{
    [super viewDidUnload];
    // Release any retained subviews of the main view.
    // e.g. self.myOutlet = nil;
}

- (BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation
{
    // Return YES for supported orientations
    return (interfaceOrientation == UIInterfaceOrientationPortrait);
}

@end
