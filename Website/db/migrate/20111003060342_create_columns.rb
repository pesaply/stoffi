# -*- encoding : utf-8 -*-
class CreateColumns < ActiveRecord::Migration
  def self.up
    create_table :columns do |t|
	  t.integer :user_id
	  t.integer :list_config_id
      t.string :name
      t.string :text
      t.string :binding
      t.string :sort_field
      t.boolean :is_always_visible
      t.boolean :is_sortable
      t.float :width
      t.boolean :is_visible
      t.string :alignment

      t.timestamps
    end
  end

  def self.down
    drop_table :columns
  end
end
