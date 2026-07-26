package com.example.test;

import android.app.Application;

import androidx.annotation.NonNull;
import androidx.lifecycle.ViewModel;
import androidx.lifecycle.ViewModelProvider;

public class MonsterViewModelFactory implements ViewModelProvider.Factory {
    private Application mApplication;

    public MonsterViewModelFactory(Application application) {
        mApplication = application;
    }

    @NonNull
    @Override
    public <T extends ViewModel> T create(@NonNull Class<T> modelClass) {
        return (T) new MonsterViewModel(mApplication);
    }
}
