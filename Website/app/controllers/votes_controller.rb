# -*- encoding : utf-8 -*-
class VotesController < ApplicationController
	oauthenticate
	respond_to :html, :mobile, :embedded, :json, :xml

	# GET /votes
	def index
		respond_with(@votes = Vote.all)
	end

	# GET /votes/1
	def show
		respond_with(@vote = Vote.find(params[:id]))
	end

	# GET /votes/new
	def new
		respond_with(@vote = Vote.new)
	end

	# GET /votes/1/edit
	def edit
		@vote = Vote.find(params[:id])
	end

	# POST /votes
	def create
		Vote.where("user_id = ? AND translation_id = ?", current_user.id, params[:vote][:translation_id]).each do |v|
			v.destroy
		end
		@vote = current_user.votes.new(params[:vote])
		@vote.translation_id = params[:vote][:translation_id]
		@vote.save
		respond_with @vote
	end

	# PUT /votes/1
	def update
		@vote = current_user.votes.find(params[:id])
		respond_with @vote
	end

	# DELETE /votes/1
	def destroy
		@vote = current_user.votes.find(params[:id])
		@vote.destroy
		respond_with @vote
	end
end
