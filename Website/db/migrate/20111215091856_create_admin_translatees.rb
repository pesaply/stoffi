# -*- encoding : utf-8 -*-
class CreateAdminTranslatees < ActiveRecord::Migration
  def change
    create_table :admin_translatees do |t|
      t.string :name
      t.text :description

      t.timestamps
    end
  end
end
