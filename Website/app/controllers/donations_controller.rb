# -*- encoding : utf-8 -*-
# The business logic for donations to artists.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

class DonationsController < ApplicationController

	respond_to :html, :mobile, :embedded, :json, :xml, :js
	
	# GET /donations
	def index
		l, o = pagination_params
		@title = t("donations.title")
		@description = t("donations.description")
		@artists = Artist.top(5, :supported)
		@users = User.top(5, :supporters)
		@pending = Donation.where(:status => :pending)
		@revoked = Donation.where(:status => :revoked)
		@charity = Donation.pending_charity_sum
		
		redirect_to :action => :new and return if params[:format] == "mobile"
		
		@channels = []
		@pending.each { |d| @channels << "user_#{d.user_id}" }
		@revoked.each { |d| @channels << "user_#{d.user_id}" }
		respond_with(@donations = Donation.limit(l).offset(o))
	end
	
	# GET /donations/by/1
	def by
		l, o = pagination_params
		params[:user_id] = process_me(params[:user_id])
		id = logged_in? ? current_user.id : -1
		
		@user = User.find(params[:user_id])
		
		if current_user != nil and params[:user_id] == current_user.id
			@donations = current_user.donations.limit(l).offset(o)
		else
			@donations = Donation.where("user_id = ?", params[:user_id]).limit(l).offset(o)
		end
		
		@channels = []
		@donations.each { |d| @channels << "user_#{d.user_id}" }
		
		respond_with(@donations)
	end

	# GET /donations/1
	def show
		@donation = Donation.find(params[:id])
		@title = t("donation.title", :artist => @donation.artist.name, :amount => number_to_currency(@donation.amount))
		@description = t("donation.description")
		respond_with(@donation, :include => [ :user, :artist ])
	end

	# GET /donations/new
	def new
		@artist = Artist.find(params[:artist_id]) if params[:artist_id]
		render :layout => params[:ajax] == nil
	end

	# GET /donations/1/edit
	def edit
		render :text => "todo" and return
		respond_with(@donation = Donation.find(params[:id]))
	end

	# POST /donations
	def create
		render :status => :forbidden and return if ["xml","json"].include?(params[:format])
	
		# donation creation by user
		if params[:create] != nil
		
			flash[:error] = ""
		
			artist = Artist.get(params[:item_name])
			
			# cannot accept for this artist
			if artist.undonatable
				logger.info "Undonatable"
				flash[:error] = t "donate.donatable_status.#{artist.donatable_status}", :artist => artist.name
				@artists = Donation.artists.paginate(:per_page => 5, :page => params[:artist_page])
				@pending = Donation.pending_artists
				@revoked = Donation.where(:status => :revoked)
				redirect_to params[:ret], :flash => { :error => flash[:error] } and return if params[:ret]
				respond_to do |format|
					format.html { render :action => "index" }
					format.mobile { render :action => "new" }
				end
			
			# either not full or artist already has some donations
			# (in which case another donation won't mean more work)
			elsif true or artist.pending > 0
				logger.info "Start donation"
				@url = Rails.env == "production" ? "www.paypal.com" : "www.sandbox.paypal.com"
				@url = "https://#{@url}/cgi-bin/webscr"
				@bus = Rails.env == "production" ? "WRZ495N8TFAWQ" : "MFFVEJ94GKGVQ"
				
				@cmd = "_donations"
				@return = params[:ret]
				@cancel_return = params[:cnc]
				@notify_url = donations_url(:format => :json)
				@no_shipping = 1
				@rm = 1
				@cbt = t("paypal.return")
				@cn = t("paypal.message")
				@bn = "Stoffi_Donate_X_SE"
				@custom = params[:custom]
				@item_number = params[:item_number]
				@item_name = params[:item_name]
				@business = Rails.env == "production" ? "WRZ495N8TFAWQ" : "MFFVEJ94GKGVQ"
				@amount = params[:amount]
				@currency_code = params[:currency_code]
				
				render :partial => "redirect"
			
			# too many pending artists
			else
				logger.info "Too many pending artists"
				flash[:error] = t "donate.full_notice"
				@artists = Donation.artists.paginate(:per_page => 5, :page => params[:artist_page])
				@pending = Donation.pending_artists
				@revoked = Donation.where(:status => :revoked)
				respond_to do |format|
					format.html { render :action => "index" }
					format.mobile { render :action => "new" }
				end
			end
		
		# PayPal IPN callback
		else
			logger.info "Validating donation with PayPal"
		
			# validate the response	with PayPal
			url = Rails.env == "production" ? "www.paypal.com" : "www.sandbox.paypal.com"
			url = "https://#{url}/webscr?cmd=_notify-validate"
			uri = URI.parse(url)
			raw = request.raw_post
			http = Net::HTTP.new(uri.host, uri.port)
			http.open_timeout = 60
			http.read_timeout = 60
			http.verify_mode = OpenSSL::SSL::VERIFY_NONE
			http.use_ssl = true
			response = http.post(uri.request_uri, raw,
				'Content-Length' => "#{raw.size}",
				'User-Agent' => 'StoffiService/0.1').body
				
			raise StandardError.new("Faulty paypal result: #{response}") unless ["VERIFIED", "INVALID"].include?(response)
			raise StandardError.new("Invalid IPN: #{response}") unless response == "VERIFIED"
			raise StandardError.new("Incomplete donation") unless params[:payment_status] == "Completed"
			
			# extract custom parameters
			h = Hash[*params[:custom].split(';').map { |x| x.split(':',2) }.flatten]
			if params['item_number'] && params['item_number'].to_i > 0
				artist = Artist.find(h['a'])
			else
				artist = Artist.get(params['item_name'])
			end
			dist = adjust_distribution(h['0'].to_i, h['1'].to_i, h['2'].to_i)

			# create donation
			@donation = Donation.new(
				:artist_id => artist.id,
				:artist_percentage => dist[:artist],
				:stoffi_percentage => dist[:stoffi],
				:charity_percentage => dist[:charity],
				:amount => params[:payment_gross].to_i,
				:return_policy => h['r'].to_i,
				:email => params[:payer_email],
				:message => params[:item_name]
			)
			@donation.user_id = h['u'] if h['u']
			success = @donation.save
			SyncController.send('create', @donation, request) if success
			
			if h['u']
				user = User.find(h['u'])
				if user
					user.links.each do |link|
						begin
							link.donate(@donation)
						rescue
							logger.error "link refused donation"
						end
					end
				end
			else
				logger.info "no user"
			end
			
			render :text => "OK"
		end
	end

	# PUT /donations/1
	def update
		render :status => :forbidden and return if ["xml","json"].include?(params[:format])
		@donation = Donation.find(params[:id])
		
		# users can only update status to "revoked"
		if not admin? and user_signed_in? and @donation.user.id == current_user.id
			s = params[:donation][:status]
			access_denied unless ['revoked'].include? s
			params[:donation].clear
			params[:donation][:status] = s
		
		# enforce admin for non-owners
		elsif not admin?
			access_denied
		end
		
		success = @donation.update_attributes(params[:donation])
		SyncController.send('update', @donation, request) if success
		respond_with(@donation)
	end

	# DELETE /donations/1
	def destroy
		render :status => :forbidden and return if ["xml","json"].include?(params[:format])
		@donation = Donation.find(params[:id])
		SyncController.send('delete', @donation, request)
		@donation.destroy
		respond_with(@donation)
	end
	
	private
	
	def adjust_distribution(to_artist,to_stoffi,to_charity)
	
		# don't allow negative numbers
		to_artist = 0 if to_artist < 0
		to_stoffi = 0 if to_stoffi < 0
		to_charity = 0 if to_charity < 0
		
		# set default if needed
		if (to_artist + to_stoffi + to_charity == 0)
			to_artist = 80
			to_stoffi = 10
			to_charity = 10
		end
		
		# adjust so sum is 100.0
		ratio = 100.0 / (to_artist + to_stoffi + to_charity)
		to_artist *= ratio
		to_stoffi *= ratio
		to_charity *= ratio
		
		return {
			:artist => to_artist,
			:stoffi => to_stoffi,
			:charity => to_charity
		}
	end
end
