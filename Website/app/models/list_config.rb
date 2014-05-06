# -*- encoding : utf-8 -*-
class ListConfig < ActiveRecord::Base
	has_many :columns
	has_many :column_sorts
	belongs_to :configuration
end
