# -*- encoding : utf-8 -*-
class CreateKeyboardShortcuts < ActiveRecord::Migration
  def self.up
    create_table :keyboard_shortcuts do |t|
	  t.integer :user_id
      t.string :name
      t.string :category
      t.string :keys
      t.boolean :is_global
      t.integer :keyboard_shortcut_profile_id

      t.timestamps
    end
  end

  def self.down
    drop_table :keyboard_shortcuts
  end
end
