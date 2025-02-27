package net.steampp.app.design.ui.viewmodels;

import androidx.lifecycle.LiveData;
import androidx.lifecycle.MutableLiveData;
import androidx.lifecycle.ViewModel;

import net.steampp.app.design.R;
import net.steampp.app.design.ui.MainApplication;

import java.util.Arrays;

public class MyViewModel extends ViewModel {

    private MutableLiveData<String> mText;

    public MyViewModel() {
        mText = new MutableLiveData<>();
        mText.setValue(Arrays.stream(this.getClass().getName().split("\\.")).reduce((first, second) -> second).orElse("").replace("ViewModel","") + "\n" + MainApplication.getContext().getString(R.string.under_construction));
    }

    public LiveData<String> getText() {
        return mText;
    }
}
