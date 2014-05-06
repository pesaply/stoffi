# -*- encoding : utf-8 -*-
class CreateLinks < ActiveRecord::Migration
  def self.up
    create_table :links do |t|
      t.integer :user_id
      t.string  :provider
      t.string  :uid
	  t.boolean :do_share, :default => true
	  t.boolean :do_listen, :default => true

      t.timestamps
    end
  end

  def self.down
    drop_table :links
  end
end
