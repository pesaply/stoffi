require 'test_helper'

class GenericBrowsingTest < ActionDispatch::IntegrationTest
	test 'show front page' do
		visit '/'
		assert page.has_link?("News"), "Missing news link"
		assert page.has_link?("Download"), "Missing download link"
		assert page.has_link?("Donate"), "Missing donate link"
		assert page.has_link?("About"), "Missing about link"
		assert page.has_link?("Login"), "Missing login link"
		#assert page.has_link?("download-button"), "Missing download button"
	end
	
	test 'go to downloads' do
		visit '/'
		click_on 'Download'
	end
	
	test 'land on downloads' do
		visit '/get'
		assert page.has_link?("Download"), "Missing download link"
		#assert page.has_link?("download-button"), "Missing download button"
	end
	
	test 'show login page' do
		visit '/login'
		assert page.has_field?("user_email", :type => "email"), "Missing email field"
		assert page.has_field?("user_plain", :type => "password"), "Missing password field"
		assert page.has_link?("Join"), "Missing join link"
	end
	
	test 'show join page' do
		visit '/join'
		assert page.has_field?("user_email", :type => "email"), "Missing email field"
		assert page.has_field?("user_plain", :type => "password"), "Missing password field"
		assert page.has_field?("user_plain_confirmation", :type => "password"), 
			"Missing password confirmation field"
		assert page.has_link?("Login"), "Missing login link"
	end
	
	test 'download on front page' do
		visit '/'
		#click_link 'download-button'
	end
	
	test 'download on download page' do
		visit '/get'
		#click_link 'download-button'
		#assert_equal download_path, current_path
	end
	
	test 'download beta' do
		visit '/get'
		within 'div#versions' do
			select('Beta', :from => 'Channel')
			click_on 'Download'
		end
	end
	
	test 'go to news' do
		visit '/'
		click_on 'News'
	end
	
	test 'go to donations' do
		visit '/'
		click_on 'Donate'
	end
end