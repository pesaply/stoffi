# -*- encoding : utf-8 -*-
class AddClientApplicationsToDevices < ActiveRecord::Migration
  def change
    add_column :devices, :client_application_id, :int
  end
end
