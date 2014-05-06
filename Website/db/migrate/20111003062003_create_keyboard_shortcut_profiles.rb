# -*- encoding : utf-8 -*-
class CreateKeyboardShortcutProfiles < ActiveRecord::Migration
  def self.up
    create_table :keyboard_shortcut_profiles do |t|
      t.string :name
      t.boolean :is_protected
      t.integer :configuration_id

      t.timestamps
    end
  end

  def self.down
    drop_table :keyboard_shortcut_profiles
  end
end
