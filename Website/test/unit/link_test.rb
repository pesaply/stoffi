require 'test_helper'

class LinkTest < ActiveSupport::TestCase
	
	def setup
		@response = Object.new
		def @response.parsed
			{ }
		end
		def @response.body
			"{}"
		end
		
		@empty_lastfm_response = { "foo" => "bar" }.to_json
		
		@listens_response = Object.new
		def @listens_response.listen=(l)
			@listen = l
		end
		def @listens_response.parsed
			{ "data" => [
				{
					"id" => "foo",
					"application" => { "id" => "foobar" },
					"data" => { "song" =>  { "url" => "something" } }
				},
				{
					"id" => "bar",
					"application" => { "id" => "foobar" },
					"data" => { "song" =>  { "url" => @listen.song.url } }
				},
			] }
		end
		@listens_response.listen = listens(:alice_one_love)
		
		@playlists_response = Object.new
		def @playlists_response.playlist=(p)
			@playlist = p
		end
		def @playlists_response.parsed
			{ "data" => [
				{
					"id" => "foo",
					"application" => { "id" => "foobar" },
					"data" => { "playlist" =>  { "url" => "something" } }
				},
				{
					"id" => "bar",
					"application" => { "id" => "foobar" },
					"data" => { "playlist" =>  { "url" => @playlist.url } }
				},
			] }
		end
		@playlists_response.playlist = playlists(:bar)
		
		creds = {
			:id => "foobar",
			:key => "somekey",
			:url => "http://example.com"
		}
		Link.any_instance.stubs(:creds).returns(creds)
	end
	
	def prepareLastFMRequest(link, verb, params)
		link = links(:alice_facebook)
		params[:api_key] = "foobar"
		params[:sk] = link.access_token
		
		if verb == :get
			params[:format] = "json"
		end
		params[:api_sig] = Digest::MD5.hexdigest(params.stringify_keys.sort.to_s + "somekey")
		if verb == :post
			params[:format] = "json"
		end
		url = "http://example.com/2.0/"
		return url, params
	end
	
	test "should create link" do
		assert_difference('Link.count', 1, "Didn't create link") do
			l = users(:bob).links.create(:provider => "facebook")
			assert_equal users(:bob).id, l.user_id
		end
	end
  
	test "should not save link without provider" do
		l = Link.new
		assert !l.save, "Created link without provider"
	end

	test "should show error" do
		assert_equal "some error", links(:alice_facebook).error
		assert_nil links(:charlie_facebook).error
	end

	test "should update credentials" do
		auth = {
			"credentials" => {
				"expires_at" => "2014-01-07 23:56",
				"token" => "sometoken",
				"secret" => "somesecretstring",
				"refresh_token" => "arefreshtoken",
			}
		}
		l = links(:alice_facebook)
		l.expects(:share) # due to backlog
		l.update_credentials(auth)
		assert_equal auth['credentials']['token'], l.access_token
		assert_equal auth['credentials']['secret'], l.access_token_secret
		assert_equal auth['credentials']['refresh_token'], l.refresh_token
		
		# check listen backlog (on lastfm link)
		l = links(:alice_lastfm)
		l.expects(:update_listen)
		l.update_credentials(auth)
	end

	test "should get facebook picture" do
		response = Object.new
		def response.parsed
			{ "picture" => { "data" => { "url" => "http://foo.com/pic.jpg" } } }
		end
		OAuth2::AccessToken.any_instance.expects(:get).returns(response)
		link = links(:alice_facebook)
		assert_equal "http://foo.com/pic.jpg", link.picture
	end

	test "should get facebook names" do
		response = Object.new
		def response.parsed
			{ "name" => "Alice Babs", "username" => "alice" }
		end
		OAuth2::AccessToken.any_instance.expects(:get).returns(response)
		names = links(:alice_facebook).names
		assert_equal "alice", names[:username]
		assert_equal "Alice Babs", names[:fullname]
	end

	test "should share song on facebook" do
		share = shares(:alice_one_love)
		params = {:params => {
			:message => "#{share.resource.title} by #{share.resource.artist.name}",
			:link => share.resource.url,
			:name => share.resource.title,
			:caption => "by #{share.resource.artist.name}",
			:source => share.resource.source,
			:picture => share.resource.picture
		}}
		OAuth2::AccessToken.any_instance.expects(:post).with('/me/feed', params).returns(@response)
		links(:alice_facebook).share(share)
	end

	test "should share playlist on facebook" do
		share = shares(:alice_foo)
		User.any_instance.expects(:name).returns("Alice Babs").at_least_once
		params = {:params => {
			:message => "#{share.resource.name} by #{share.user.name}",
			:link => share.resource.url,
			:name => share.resource.name,
			:caption => "A playlist on Stoffi",
			:picture => share.resource.picture
		}}
		OAuth2::AccessToken.any_instance.expects(:post).with('/me/feed', params).returns(@response)
		links(:alice_facebook).share(share)
	end

	test "should share song on twitter" do
		share = shares(:alice_one_love)
		params = {
			:status => "#{share.resource.title} by #{share.resource.artist.name} #{share.resource.url}",
		}
		url = "http://example.com/1.1/statuses/update.json"
		OAuth::AccessToken.any_instance.expects(:request).with(:post, url, params).returns(@response)
		links(:alice_twitter).share(share)
	end

	test "should share playlist on twitter" do
		share = shares(:alice_foo)
		User.any_instance.expects(:name).returns("Alice Babs").at_least_once
		params = {
			:status => "#{share.resource.name} by #{share.user.name} #{share.resource.url}",
		}
		url = "http://example.com/1.1/statuses/update.json"
		OAuth::AccessToken.any_instance.expects(:request).with(:post, url, params).returns(@response)
		links(:alice_twitter).share(share)
	end

	test "should start listen on facebook" do
		listen = listens(:alice_one_love)
		params = {:params => {
			:song => listen.song.url,
			:end_time => listen.ended_at,
			:start_time => listen.created_at,
		}}
		url = "/me/music.listens"
		OAuth2::AccessToken.any_instance.expects(:post).with(url, params).returns(@response)
		links(:alice_facebook).start_listen(listen)
	end

	test "should start listen on last.fm" do
		listen = listens(:alice_one_love)
		link = links(:alice_lastfm)
		params = {
			:artist => listen.song.artist.name,
			:track => listen.song.title,
			:duration => listen.song.length.to_i,
			:timestamp => listen.created_at.to_i,
			:method => "track.updateNowPlaying"
		}
		url, params = prepareLastFMRequest(link, :post, params)
		stub_request(:any, url).with(params).to_return(:body => @empty_lastfm_response)
		link.start_listen(listen)
	end

	test "should update listen on facebook" do
		listen = listens(:alice_not_afraid)
		
		# find listen
		url = "/me/music.listens?limit=25&offset=0"
		@listens_response.listen = listen
		OAuth2::AccessToken.any_instance.expects(:get).with(url).returns(@listens_response)
		
		# update listen
		params = { :params => { :end_time => listen.ended_at }}
		url = "/bar"
		OAuth2::AccessToken.any_instance.expects(:post).with(url, params).returns(@response)
		links(:alice_facebook).update_listen(listen)
	end

	test "should end listen on facebook" do
		listen = listens(:alice_one_love)
		
		# find listen
		url = "/me/music.listens?limit=25&offset=0"
		@listens_response.listen = listen
		OAuth2::AccessToken.any_instance.expects(:get).with(url).returns(@listens_response)
		
		# delete listen
		url = "/bar"
		OAuth2::AccessToken.any_instance.expects(:delete).with(url).returns(@response)
		links(:alice_facebook).end_listen(listen)
	end

	test "should end listen on last.fm" do
		listen = listens(:alice_one_love)
		link = links(:alice_lastfm)
		params = {
			:artist => listen.song.artist.name,
			:track => listen.song.title,
			:duration => listen.song.length.to_i,
			:timestamp => listen.created_at.to_i,
			:method => "track.scrobble"
		}
		url, params = prepareLastFMRequest(link, :post, params)
		stub_request(:any, url).with(params).to_return(:body => @empty_lastfm_response)
		link.end_listen(listen)
	end

	test "should delete listen on facebook" do
		listen = listens(:alice_one_love)
		
		# find listen
		url = "/me/music.listens?limit=25&offset=0"
		@listens_response.listen = listen
		OAuth2::AccessToken.any_instance.expects(:get).with(url).returns(@listens_response)
		
		# delete listen
		url = "/bar"
		OAuth2::AccessToken.any_instance.expects(:delete).with(url).returns(@response)
		links(:alice_facebook).delete_listen(listen)
	end

	test "should create playlist on facebook" do
		playlist = playlists(:foo)
		params = { :params => { :playlist => playlist.url } }
		url = "/me/music.playlists"
		OAuth2::AccessToken.any_instance.expects(:post).with(url, params).returns(@response)
		links(:alice_facebook).create_playlist(playlist)
	end

	test "should update playlist on facebook" do
		playlist = playlists(:bar)
		
		# find playlist
		url = "/me/music.playlists?limit=25&offset=0"
		OAuth2::AccessToken.any_instance.expects(:get).with(url).returns(@playlists_response)
		
		# update playlist
		url = "/?id=bar&scrape=true"
		OAuth2::AccessToken.any_instance.expects(:get).with(url).returns(@response)
		links(:alice_facebook).update_playlist(playlist)
	end

	test "should update new playlist on facebook" do
		playlist = playlists(:foo)
		
		# find playlist
		url = "/me/music.playlists?limit=25&offset=0"
		OAuth2::AccessToken.any_instance.expects(:get).with(url).returns(@playlists_response)
		
		# create playlist
		params = { :params => { :playlist => playlist.url } }
		url = "/me/music.playlists"
		OAuth2::AccessToken.any_instance.expects(:post).with(url, params).returns(@response)
		links(:alice_facebook).update_playlist(playlist)
	end

	test "should delete playlist on facebook" do
		playlist = playlists(:bar)
		
		# find playlist
		url = "/me/music.playlists?limit=25&offset=0"
		OAuth2::AccessToken.any_instance.expects(:get).with(url).returns(@playlists_response)
		
		# update playlist
		url = "/bar"
		OAuth2::AccessToken.any_instance.expects(:delete).with(url).returns(@response)
		links(:alice_facebook).delete_playlist(playlist)
	end
end
