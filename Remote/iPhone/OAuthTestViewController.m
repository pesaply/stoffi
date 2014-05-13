//
//  OAuthTestViewController.m
//  StoffiRemote
//
//  Created by Fredrik Gadnell on 9/19/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "OAuthTestViewController.h"

@implementation OAuthTestViewController

- (id)initWithNibName:(NSString *)nibNameOrNil bundle:(NSBundle *)nibBundleOrNil
{
    self = [super initWithNibName:nibNameOrNil bundle:nibBundleOrNil];
    if (self) {
        self.view.backgroundColor = [UIColor darkGrayColor];
        
        UIButton *loginButton = [UIButton buttonWithType:UIButtonTypeRoundedRect];
        loginButton.frame = CGRectMake(100, 100, 80, 40);
        [loginButton setTitle:@"Login" forState:UIControlStateNormal];
        [loginButton addTarget:self action:@selector(loginButtonPressed) forControlEvents:UIControlEventTouchUpInside];
        
        UIButton *requestButton = [UIButton buttonWithType:UIButtonTypeRoundedRect];
        requestButton.frame = CGRectMake(100, 160, 80, 40);
        [requestButton setTitle:@"Request" forState:UIControlStateNormal];
        [requestButton addTarget:self action:@selector(requestButtonPressed) forControlEvents:UIControlEventTouchUpInside];
        
        [self.view addSubview:loginButton];
        [self.view addSubview:requestButton];
    }
    return self;
}

- (void)loginButtonPressed {
    [[StoffiOAuthManager sharedManager] signInToStoffiWithDelegate:self];
}

- (void)requestButtonPressed {
    RESTRequest *request = [[RESTClient sharedClient] get:@"/share" delegate:self];
    request.shouldLog = YES;
}

- (void)restRequestDidFail {
    NSLog(@"OAuthTestVC: Did receive callback from RestRequest: Request failed");
}

- (void)restRequestDidLoadResult:(id)jsonObject {
    NSLog(@"OAuthTestVC: Did receive callbak from RestRequest: %@", jsonObject);
}

- (void)viewController:(GTMOAuthViewControllerTouch *)viewController
      finishedWithAuth:(GTMOAuthAuthentication *)auth
                 error:(NSError *)error 
{
    if (error != nil) {
        // Authentication failed
        NSLog(@"Auth failed");
    } else {
        // Authentication succeeded
        NSLog(@"Auth succeeded");
    }
}

- (void)didLogin {
    NSLog(@"OAuthTestViewController did login!");
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

#pragma mark - View lifecycle

/*
// Implement loadView to create a view hierarchy programmatically, without using a nib.
- (void)loadView
{
}
*/

/*
// Implement viewDidLoad to do additional setup after loading the view, typically from a nib.
- (void)viewDidLoad
{
    [super viewDidLoad];
}
*/

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
