# -*- encoding : utf-8 -*-
class CreateColumnSorts < ActiveRecord::Migration
  def self.up
    create_table :column_sorts do |t|
	  t.integer :user_id
      t.integer :column_id
      t.string :field
      t.boolean :ascending

      t.timestamps
    end
  end

  def self.down
    drop_table :column_sorts
  end
end
