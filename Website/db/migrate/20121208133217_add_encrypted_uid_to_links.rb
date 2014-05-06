# -*- encoding : utf-8 -*-
class AddEncryptedUidToLinks < ActiveRecord::Migration
  def change
    add_column :links, :encrypted_uid, :string

  end
end
