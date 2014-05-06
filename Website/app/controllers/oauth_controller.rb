# -*- encoding : utf-8 -*-
require 'oauth/controllers/provider_controller'
class OauthController < ApplicationController
	include OAuth::Controllers::ProviderController
	
	def authorize
		# look for existing access token for app
		# if the user has already authorized the app
		# we should just pass it on and not ask again
		# for authorization
		if request.get? and params[:oauth_token]
			@token = ::RequestToken.find_by_token! params[:oauth_token]
			if @token and !@token.invalidated?
			
				# find access token for the app
				token = current_user.tokens.where(
					"client_application_id = ? AND type = 'AccessToken' AND invalidated_at IS NULL AND authorized_at IS NOT NULL",
					@token.client_application.id).first
					
				# check the access token
				if token and !token.invalidated?
					@token.authorize!(current_user)
				
					# redirect according to request token
					callback_url  = @token.oob? ? @token.client_application.callback_url : @token.callback_url
					@redirect_url = URI.parse(callback_url) unless callback_url.blank?

					unless @redirect_url.to_s.blank?
						@redirect_url.query = @redirect_url.query.blank? ?
							"oauth_token=#{@token.token}&oauth_verifier=#{@token.verifier}" :
							@redirect_url.query + "&oauth_token=#{@token.token}&oauth_verifier=#{@token.verifier}"
						redirect_to @redirect_url.to_s
					else
						render :action => "authorize_success"
					end
					return
				end
			end
		end
		super
	end

	protected
	
	# Override this to match your authorization page form
	# It currently expects a checkbox called authorize
	def user_authorizes_token?
		params[:authorize] == '1'
	end

	# should authenticate and return a user if valid password.
	# This example should work with most Authlogic or Devise.
	def authenticate_user(username,password)
		user = User.find_by_email params[:username]
		if user && user.valid_password?(params[:password])
			user
		else
			nil
		end
	end

end
