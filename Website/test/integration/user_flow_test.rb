require 'test_helper'

class UserFlowTest < ActionDispatch::IntegrationTest
	fixtures :users
	
	def set_pass(user, passwd)
		params = { :password => passwd, :password_confirmation => passwd }
		assert user.update_with_password(params), "Could not change password"
		assert user.valid_password?(passwd), "Password did not change properly"
	end
	
	test "login and browse" do
		get login_path
		assert_response :success
		
		user = users(:alice)
		passwd = "foobar"
		set_pass(user, passwd)
		
		post_via_redirect login_path, user: { email: user.email, password: passwd }
		assert_equal dashboard_path, path
		
		get profile_path
		assert_response :success
		assert assigns(:user)
		assert_equal assigns(:user).id, user.id
	end
	
	test "login and continue" do
		get login_path, {}, {'HTTP_REFERER' => news_path}
		assert_response :success
		
		user = users(:alice)
		passwd = "foobar"
		set_pass(user, passwd)
		
		post_via_redirect login_path, user: { email: user.email, password: passwd }
		assert_equal news_path, path
	end
	
	test "redirect to login and continue" do
		get_via_redirect profile_path
		assert_response :success
		assert_equal login_path, path
		
		# referer isn't set yet so we need to re-request with the header
		get login_path, {}, {'HTTP_REFERER' => profile_path}
		assert session['user_return_to'], "Return path not set"
		
		user = users(:alice)
		passwd = "foobar"
		set_pass(user, passwd)
		
		post_via_redirect login_path, user: { email: user.email, password: passwd }
		assert_equal profile_path, path
	end
	
	test "fail login with wrong password" do
		get login_path
		assert_response :success
		
		user = users(:alice)
		passwd = "foobar"
		
		post_via_redirect login_path, user: { email: user.email, password: passwd }
		assert_equal login_path, path
		assert_equal "Invalid email or password.", flash[:alert]
	end
	
	test "fail login with wrong email" do
		get login_path
		assert_response :success
		
		user = users(:alice)
		passwd = "foobar"
		set_pass(user, passwd)
		
		post_via_redirect login_path, user: { email: "foobar", password: passwd }
		assert_equal login_path, path
		assert_equal "Invalid email or password.", flash[:alert]
	end
	
	test "logout" do
		user = users(:alice)
		passwd = "foobar"
		set_pass(user, passwd)
		post_via_redirect login_path, user: { email: user.email, password: passwd }
		
		get_via_redirect logout_path
		assert_response :success
		assert_equal login_path, path
		
		get profile_path
		assert_response :redirect
	end
	
	test "logout and continue" do
		user = users(:alice)
		passwd = "foobar"
		set_pass(user, passwd)
		post_via_redirect login_path, user: { email: user.email, password: passwd }
		
		get_via_redirect logout_path, {}, {'HTTP_REFERER' => news_path}
		assert_response :success
		assert_equal news_path, path
	end
end
