require 'test_helper'

class PlaylistsControllerTest < ActionController::TestCase
	include Devise::TestHelpers
	setup do
		@playlist = users(:alice).playlists.first
		@playlist2 = users(:bob).playlists.first
		@user = users(:alice)
		@request.env["devise.mapping"] = Devise.mappings[:user]
	end

	test "should get index logged in" do
		sign_in @user
		get :index
		assert_response :success
		assert_not_nil assigns(:personal)
		assert_not_nil assigns(:global)
		assert_equal @user.playlists.count, assigns(:personal).count
	end

	test "should get index logged out" do
		get :index
		assert_response :success
		assert_nil assigns(:personal)
		assert_not_nil assigns(:global)
	end

	test "should show" do
		get :show, :id => @playlist.to_param
		assert_response :found
	end

	test "should not show" do
		get :show, :id => "foobar"
		assert_response :not_found
	end

	test "should not get new logged out" do
		get :new
		assert_redirected_to login_url, "Not redirected to login page"
	end

	test "should get new" do
		sign_in @user
		get :new
		assert_response :success
	end

	test "should create playlist" do
		Link.any_instance.stubs(:create_playlist).returns(nil)
		sign_in @user
		songs = [
			{:path => "foo"},
			{:path => "bar"},
		]
		assert_difference('Playlist.count') do
			post :create, { :playlist => { :name => "Something" }, :songs => songs }
		end
		
		assert_not_nil assigns(:playlist)
		assert_redirected_to playlist_path(assigns(:playlist))
		assert_equal 2, assigns(:playlist).songs.count
	end

	test "should not create playlist logged out" do
		post :create, :playlist => @playlist.attributes
		assert_redirected_to login_url, "Not redirected to login page"
	end

	test "should not create two playlists with same name" do
		Link.any_instance.stubs(:update_playlist).returns(nil)
		sign_in @user
		songs = [
			{:path => "stoffi:track:youtube:abc"},
			{:path => "foo"},
		]
		assert_no_difference('Playlist.count') do
			post :create, {:playlist => @playlist.attributes, :songs => songs}
		end
		
		assert_not_nil assigns(:playlist)
		assert_redirected_to playlist_path(assigns(:playlist))
		assert_equal 3, assigns(:playlist).songs.count
	end

	test "should get edit" do
		sign_in @user
		get :edit, :id => @playlist.to_param
		assert_response :success
	end

	test "should not get edit logged out" do
		get :edit, :id => @playlist.to_param
		assert_redirected_to login_url, "Not redirected to login page"
	end

	test "should not get edit for someone else's playlist" do
		sign_in @user
		get :edit, :id => @playlist2.to_param
		assert_response :not_found
	end

	test "should update playlist" do
		Link.any_instance.stubs(:update_playlist).returns(nil)
		sign_in @user
		put :update, :id => @playlist.to_param, :playlist => @playlist.attributes
		assert_response :found
		assert_redirected_to playlist_path(assigns(:playlist))
	end

	test "should not update playlist logged out" do
		put :update, :id => @playlist.to_param, :playlist => @playlist.attributes
		assert_redirected_to login_url, "Not redirected to login page"
	end

	test "should not update someone else's playlist" do
		sign_in @user
		put :update, :id => @playlist2.to_param, :playlist => @playlist2.attributes
		assert_response :not_found
	end

	test "should destroy playlist" do
		Link.any_instance.stubs(:delete_playlist).returns(nil)
		sign_in @user
		assert_difference('Playlist.count', -1) do
			delete :destroy, :id => @playlist.to_param
		end
		assert_redirected_to playlists_path
	end

	test "should not destroy playlist logged out" do
		delete :destroy, :id => @playlist.to_param
		assert_redirected_to login_url, "Not redirected to login page"
	end
	
	test "should follow playlist" do
		sign_in @user
		put :follow, :id => @playlist2.to_param
		assert_response :found
		assert @playlist2.subscribers.exists?(@user.id), "Not subscribed to playlist"
	end

	test "should unfollow playlist" do
		sign_in @user
		put :follow, :id => @playlist2.to_param
		assert_redirected_to playlist_path(@playlist2), "Could not follow playlist"
		assert @playlist2.subscribers.exists?(@user.id), "Not subscribed to playlist"
		delete :destroy, :id => @playlist2.to_param
		assert_redirected_to playlist_path(@playlist2), "Could not unfollow playlist"
		get :show, :id => @playlist2.to_param
		assert_response :success, "Could not get playlist"
		assert !(@playlist2.subscribers.exists?(@user.id)), "Still subscribing to playlist"
	end
end