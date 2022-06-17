﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace OfficialPlugins.Common.Controls
{
    /// <summary>
    /// Interaction logic for ColorDisplay.xaml
    /// </summary>
    public partial class ColorDisplay : UserControl, IDataUi
    {
        public ColorDisplay()
        {
            InitializeComponent();
        }

        InstanceMember mInstanceMember;


        public InstanceMember InstanceMember
        {
            get
            {
                return mInstanceMember;
            }
            set
            {
                bool instanceMemberChanged = mInstanceMember != value;
                if (mInstanceMember != null && instanceMemberChanged)
                {
                    mInstanceMember.PropertyChanged -= HandlePropertyChange;
                }
                mInstanceMember = value;
                if (mInstanceMember != null && instanceMemberChanged)
                {
                    mInstanceMember.PropertyChanged += HandlePropertyChange;
                }
                Refresh();
            }
        }
        public bool SuppressSettingProperty { get; set; }


        public void Refresh(bool forceRefreshEvenIfFocused = false)
        {
            SuppressSettingProperty = true;


            object valueOnInstance;
            bool successfulGet = this.TryGetValueOnInstance(out valueOnInstance);

            if (successfulGet)
            {
                var wasSet = TrySetValueOnUi(valueOnInstance) == ApplyValueResult.Success;
            }

            SuppressSettingProperty = false;
        }

        public ApplyValueResult TryGetValueOnUi(out object result)
        {
            result = this.ColorPickerPanel.SelectedColor;

            return ApplyValueResult.Success;
        }

        public ApplyValueResult TrySetValueOnUi(object value)
        {
            if(value is Color color)
            {
                this.ColorPickerPanel.SelectedColor = color;
                return ApplyValueResult.Success;
            }
            else
            {
                return ApplyValueResult.NotSupported;
            }
        }

        private void HandlePropertyChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InstanceMember.Value))
            {
                this.Refresh();

            }
        }
    }
}