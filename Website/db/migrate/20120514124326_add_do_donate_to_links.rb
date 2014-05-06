# -*- encoding : utf-8 -*-
class AddDoDonateToLinks < ActiveRecord::Migration
  def change
    add_column :links, :do_donate, :boolean, :default => true
  end
end
