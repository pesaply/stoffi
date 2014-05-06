# -*- encoding : utf-8 -*-
class ContributeController < ApplicationController

	before_filter :set_title_and_description, :except => :index

	def index
		@description = t("contribute.description")
		@title = t("contribute.title")
	end

	def test
	end

	def design
	end

	def hack
	end

	def promote
	end

	def idea
	end

	def write
	end

	def apps
	end

	def plugins
	end

	def translate
	end
	
	private
	
	def set_title_and_description
		@title = t("contribute.#{action_name}.title")
		@description = t("contribute.#{action_name}.description")
	end

end
