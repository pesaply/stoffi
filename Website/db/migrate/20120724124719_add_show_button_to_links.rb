# -*- encoding : utf-8 -*-
class AddShowButtonToLinks < ActiveRecord::Migration
  def change
    add_column :links, :show_button, :boolean, :default => true
  end
end
