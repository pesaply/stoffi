require 'test_helper'

class Users::RegistrationsControllerTest < ActionController::TestCase
	include Devise::TestHelpers
	setup do
		@user = users(:alice)
		@request.env["devise.mapping"] = Devise.mappings[:user]
	end

	test "should get profile while logged in" do
		sign_in @user
		get :show
		assert_response :success
	end
	
	test "should delete profile" do
		sign_in @user
		
		u = User.find(@user.id)
		
		assert_difference('User.count', -1) do
		assert_difference('Playlist.count', @user.playlists.count * -1) do
		assert_difference('Listen.count', @user.listens.count * -1) do
		assert_difference('Share.count', @user.shares.count * -1) do
		assert_difference('Link.count', @user.links.count * -1) do
		assert_difference('Device.count', @user.devices.count * -1) do
		assert_difference('ClientApplication.count', @user.apps.count * -1) do
		assert_difference('users(:bob).playlist_subscriptions.count', -1) do
			delete :destroy
			assert_redirected_to login_path, "Not redirected to login page"
		end end end end end end end end
		
		assert_raises ActiveRecord::RecordNotFound do
			User.find(@user.id)
		end
		
		# TODO: possible to make this prettier?
		Playlist.all.each do |playlist|
			assert playlist.subscribers.where(id: @user.id).empty?
		end
	end
end