# -*- encoding : utf-8 -*-
class QueuesController < ApplicationController
	# GET /queues
	# GET /queues.xml
	def index
		@queues = Queue.all

		respond_to do |format|
			format.html # index.html.erb
			format.xml  { render :xml => @queues }
		end
	end

	# GET /queues/1
	# GET /queues/1.xml
	def show
		@queue = Queue.find(params[:id])

		respond_to do |format|
			format.html # show.html.erb
			format.xml  { render :xml => @queue }
		end
	end

	# GET /queues/new
	# GET /queues/new.xml
	def new
		@queue = Queue.new

		respond_to do |format|
			format.html # new.html.erb
			format.xml  { render :xml => @queue }
		end
	end

	# GET /queues/1/edit
	def edit
		@queue = Queue.find(params[:id])
	end

	# POST /queues
	# POST /queues.xml
	def create
		@queue = Queue.new(params[:queue])

		respond_to do |format|
			if @queue.save
				format.html { redirect_to(@queue, :notice => 'Queue was successfully created.') }
				format.xml  { render :xml => @queue, :status => :created, :location => @queue }
			else
				format.html { render :action => "new" }
				format.xml  { render :xml => @queue.errors, :status => :unprocessable_entity }
			end
		end
	end

	# PUT /queues/1
	# PUT /queues/1.xml
	def update
		@queue = Queue.find(params[:id])

		respond_to do |format|
			if @queue.update_attributes(params[:queue])
				format.html { redirect_to(@queue, :notice => 'Queue was successfully updated.') }
				format.xml  { head :ok }
			else
				format.html { render :action => "edit" }
				format.xml  { render :xml => @queue.errors, :status => :unprocessable_entity }
			end
		end
	end

	# DELETE /queues/1
	# DELETE /queues/1.xml
	def destroy
		@queue = Queue.find(params[:id])
		@queue.destroy

		respond_to do |format|
			format.html { redirect_to(queues_url) }
			format.xml  { head :ok }
		end
	end
end
