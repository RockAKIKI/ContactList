﻿using WpfUI.Services;
using WpfUI.Utilities;
using DataAccessLibrary;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using DataAccessLibrary.Entities;
using Microsoft.EntityFrameworkCore;
using DataAccessLibrary.Models;

namespace WpfUI.ViewModels;

public class ContactsViewModel : ViewModelBase
{
    private readonly ContactDbContextFactory _dbContext;
    private readonly IDialogService _dialogService;

    public ContactsViewModel(ContactDbContextFactory dbContext,
                             IDialogService dialogService)
    {
        _dbContext = dbContext;
        _dialogService = dialogService;
        CreateContactCommand = new RelayCommand(CreateContact);
        EditContactCommand = new RelayCommand(EditContact, CanEdit);
        AddPhoneNumber = new RelayCommand(AddContactPhone, IsEdit);
        AddEmailAddress = new RelayCommand(AddContactEmail, IsEdit);
        AddPhysicalAddress = new RelayCommand(AddContactAddress, IsEdit);
        RemovePhoneNumber = new RelayCommand<int>(RemoveContactPhone);
        RemoveEmailAddress = new RelayCommand<int>(RemoveContactEmail);
        RemovePhysicalAddress = new RelayCommand<int>(RemoveContactAddress);
        UpdateContactCommand = new RelayCommand(UpdateContact, IsEdit);
        UpdateContactImageCommand = new RelayCommand(UpdateContactImage, IsEdit);
        FavoriteContactCommand = new RelayCommand(FavoriteContact);
        DeleteContactCommand = new RelayCommand(DeleteContact, CanDelete);
    }

    private bool _isEditMode;
    public bool IsEditMode
    {
        get
        {
            return _isEditMode;
        }
        set
        {
            OnPropertyChanged(ref _isEditMode, value);
            OnPropertyChanged(nameof(IsDisplayMode));
        }
    }

    public bool IsDisplayMode
    {
        get { return !_isEditMode; }
    }

    public ObservableCollection<PersonModel> Contacts { get; set; }

    private PersonModel _selectedContact;

    public PersonModel SelectedContact
    {
        get
        {
            return _selectedContact;
        }
        set
        {
            OnPropertyChanged(ref _selectedContact, value);
        }
    }

    public ICommand EditContactCommand { get; private set; }
    public ICommand AddPhoneNumber { get; private set; }
    public ICommand AddEmailAddress { get; private set; }
    public ICommand AddPhysicalAddress { get; private set; }
    public ICommand RemovePhoneNumber { get; private set; }
    public ICommand RemoveEmailAddress { get; private set; }
    public ICommand RemovePhysicalAddress { get; private set; }
    public ICommand UpdateContactCommand { get; private set; }
    public ICommand FavoriteContactCommand { get; private set; }
    public ICommand UpdateContactImageCommand { get; private set; }
    public ICommand CreateContactCommand { get; private set; }
    public ICommand DeleteContactCommand { get; private set; }

    public void LoadContacts(IEnumerable<PersonModel> contacts)
    {
        Contacts = new ObservableCollection<PersonModel>(contacts);
        OnPropertyChanged(nameof(Contacts));
    }

    private bool CanEdit()
    {
        if (SelectedContact == null)
        {
            return false;
        }

        return !IsEditMode;
    }

    private bool CanDelete()
    {
        return SelectedContact == null ? false : true;
    }

    private void EditContact()
    {
        IsEditMode = true;
    }

    private bool IsEdit()
    {
        return IsEditMode;
    }

    private void CreateContact()
    {
        Person newContact = new()
        {
            FirstName = "Firstname",
            LastName = "Lastname"
        };

        using ContactDbContext db = _dbContext.CreateDbContext();
        db.Contacts.Add(newContact);
        db.SaveChanges();

        PersonModel p = PersonModel.ToPersonModelMap(newContact);
        Contacts.Add(p);
        SelectedContact = p;
    }

    private void UpdateContact()
    {
        using ContactDbContext db = _dbContext.CreateDbContext();
        Person person = db.Contacts
            .Include(x => x.PhoneNumbers)
            .Include(x => x.EmailAddresses)
            .Include(x => x.Addresses)
            .AsSplitQuery()
            .FirstOrDefault(x => x.Id == SelectedContact.Id);

        if (person != null)
        {
            person.FirstName = SelectedContact.FirstName;
            person.LastName = SelectedContact.LastName;
            person.Addresses = new ObservableCollection<Address>(SelectedContact.Addresses.ToList().Select(x => AddressModel.ToAddressMap(x)));
            person.PhoneNumbers = new ObservableCollection<Phone>(SelectedContact.PhoneNumbers.ToList().Select(x => PhoneModel.ToPhoneMap(x)));
            person.EmailAddresses = new ObservableCollection<Email>(SelectedContact.EmailAddresses.ToList().Select(x => EmailModel.ToEmailMap(x)));
            person.ImagePath = SelectedContact.ImagePath;
            person.IsFavorite = SelectedContact.IsFavorite;
            db.SaveChanges();
        }

        IsEditMode = false;
        OnPropertyChanged(nameof(SelectedContact));
    }
    private void AddContactPhone()
    {
        SelectedContact.PhoneNumbers.Add(new PhoneModel());
    }
    private void AddContactEmail()
    {
        SelectedContact.EmailAddresses.Add(new EmailModel());
    }

    private void AddContactAddress()
    {
        SelectedContact.Addresses.Add(new AddressModel());
    }
    private void RemoveContactPhone(int id)
    {
        PhoneModel p = SelectedContact.PhoneNumbers.FirstOrDefault(x => x.Id == id);

        if (p is not null)
        {
            SelectedContact.PhoneNumbers.Remove(p);
        }
    }
    private void RemoveContactEmail(int id)
    {
        EmailModel e = SelectedContact.EmailAddresses.FirstOrDefault(x => x.Id == id);

        if (e is not null)
        {
            SelectedContact.EmailAddresses.Remove(e);
        }
    }

    private void RemoveContactAddress(int id)
    {
        AddressModel a = SelectedContact.Addresses.FirstOrDefault(x => x.Id == id);

        if (a is not null)
        {
            SelectedContact.Addresses.Remove(a);
        }
    }

    private void UpdateContactImage()
    {
        string filePath = _dialogService.OpenFile("Image files|*.bmp;*.jpg;*.jpeg;*.png;|All files");
        SelectedContact.ImagePath = filePath;

        using ContactDbContext db = _dbContext.CreateDbContext();
        Person person = db.Contacts.FirstOrDefault(x => x.Id == SelectedContact.Id);

        if (person != null)
        {
            person.ImagePath = SelectedContact.ImagePath;
            db.SaveChanges();
        }

        OnPropertyChanged(nameof(SelectedContact));
    }

    private void FavoriteContact()
    {
        using ContactDbContext db = _dbContext.CreateDbContext();
        Person person = db.Contacts.FirstOrDefault(x => x.Id == SelectedContact.Id);

        if (person != null)
        {
            person.IsFavorite = SelectedContact.IsFavorite;
            db.SaveChanges();
        }

        OnPropertyChanged(nameof(SelectedContact));
    }

    private void DeleteContact()
    {
        using ContactDbContext db = _dbContext.CreateDbContext();
        Person person = db.Contacts.FirstOrDefault(x => x.Id == SelectedContact.Id);

        if (person != null)
        {
            db.Contacts.Remove(person);
            db.SaveChanges();
        }

        Contacts.Remove(SelectedContact);
        SelectedContact = null;
        IsEditMode = false;
    }
}
