require 'test_helper'

class PlaylistTest < ActiveSupport::TestCase
  test "should create playlist" do
    assert_difference('Playlist.count', 1, "Didn't create playlist") do
      p = users(:alice).playlists.create(:name => "Something")
      assert_equal users(:alice).id, p.user_id, "Didn't assign the playlist to the correct user"
    end
  end
  
  test "should not save playlist without name" do
    p = users(:alice).playlists.new()
    assert !p.save, "Created playlist wihtout a name"
  end
  
  test "should not save playlist without user" do
    p = Playlist.new(:name => "Something")
    assert !p.save, "Created playlist not belonging to a user"
  end

  test "should not create two playlists with same name" do
    assert_no_difference('Playlist.count', "Created two playlists with same name") do
      users(:alice).playlists.create(:name => "Foo")
    end
  end

  test "playlist name uniqness should be scoped" do
    assert_difference('Playlist.count', 1, "Didn't allow two users to have playlists with same name") do
      users(:bob).playlists.create(:name => "Foo")
    end
  end
end
