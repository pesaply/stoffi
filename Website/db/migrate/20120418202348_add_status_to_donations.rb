# -*- encoding : utf-8 -*-
class AddStatusToDonations < ActiveRecord::Migration
  def change
    add_column :donations, :status, :string, :default => "pending"
  end
end
