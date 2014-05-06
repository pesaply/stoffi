require 'test_helper'

class UserTest < ActiveSupport::TestCase
  test "should create user" do
		passwd = "foobar"
    assert_difference('User.count', 1, "Didn't create user") do
      User.create(:email => "foo@bar.com", :password => passwd, :password_confirmation => passwd)
    end
  end

  test "should not save user with short password" do
		passwd = "foo"
    assert_no_difference('User.count', "Created user with short password") do
      User.create(:email => "foo@bar.com", :password => passwd, :password_confirmation => passwd)
    end
  end

  test "should not save user without password" do
    assert_no_difference('User.count', "Created user without password") do
      User.create(:email => "foo@bar.com")
    end
  end

	test "should destroy user" do
		alice = users(:alice)
		bob = users(:bob)
		assert_difference('User.count', -1, "Didn't remove user") do
			assert_difference('bob.playlist_subscriptions.count', -1, "Didn't remove playlist subscription") do
				assert_difference('Playlist.count', -1*alice.playlists.count, "Didn't remove playlists") do
					alice.destroy
				end
			end
		end
	end
end
