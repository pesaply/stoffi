# -*- encoding : utf-8 -*-
class CreateLinkBacklogs < ActiveRecord::Migration
  def change
    create_table :link_backlogs do |t|
      t.integer :link_id
      t.integer :resource_id
      t.string :resource_type
      t.string :error

      t.timestamps
    end
  end
end
