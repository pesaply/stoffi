# -*- encoding : utf-8 -*-
class AddReturnToDonations < ActiveRecord::Migration
  def change
    add_column :donations, :return_policy, :integer, :default => 0
  end
end
