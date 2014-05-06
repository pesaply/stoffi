# -*- encoding : utf-8 -*-
class AddAdSettingsToUsers < ActiveRecord::Migration
  def change
    add_column :users, :show_ads, :string, :default => "all"
  end
end
