# -*- encoding : utf-8 -*-
class CreateTranslations < ActiveRecord::Migration
  def change
    create_table :translations do |t|
      t.integer :language_id
      t.integer :translatee_id
      t.integer :user_id
      t.text :content

      t.timestamps
    end
  end
end
