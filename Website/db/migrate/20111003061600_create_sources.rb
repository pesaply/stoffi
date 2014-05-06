# -*- encoding : utf-8 -*-
class CreateSources < ActiveRecord::Migration
  def self.up
    create_table :sources do |t|
	  t.integer :user_id
	  t.integer :configuration_id
      t.string :type
      t.string :data
      t.boolean :include

      t.timestamps
    end
  end

  def self.down
    drop_table :sources
  end
end
