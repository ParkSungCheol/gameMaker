package com.example.test;

import android.app.Activity;
import android.os.AsyncTask;
import android.os.Build;

import androidx.annotation.RequiresApi;
import androidx.lifecycle.ViewModelProvider;
import androidx.lifecycle.ViewModelStoreOwner;

import java.lang.ref.WeakReference;

public abstract class DBPrePopulateNotifier {
    private Activity activity;
    public static MonsterViewModel monsterViewModel;

    public DBPrePopulateNotifier(Activity activity) {
        this.activity = activity;
        monsterViewModel = new ViewModelProvider((ViewModelStoreOwner) activity, new MonsterViewModelFactory(activity.getApplication())).get(MonsterViewModel.class);
    }

    public void execute() {
        new WaitDBToPrePopulateAsyncTask(this, activity).execute();
    }

    public abstract void onFinished();

    private static class WaitDBToPrePopulateAsyncTask extends AsyncTask<Void, Void, String> {
        private WeakReference<Activity> weakReference;
        private static final int SLEEP_BY_MILLISECONDS = 10000;
        private DBPrePopulateNotifier notifier;

        private WaitDBToPrePopulateAsyncTask(DBPrePopulateNotifier notifier, Activity activity) {
            weakReference = new WeakReference<>(activity);
            this.notifier = notifier;
        }

        @RequiresApi(api = Build.VERSION_CODES.N)
        @Override
        protected String doInBackground(Void... voids) {
            int count;
            Activity activity;
            while(true) {
                try {
                    Thread.sleep(SLEEP_BY_MILLISECONDS);
                }
                catch(InterruptedException e) {
                    e.printStackTrace();
                    break;
                }

                activity = weakReference.get();
                if(activity == null || activity.isFinishing()) {
                    return null;
                }

                // TODO DB...
                count = monsterViewModel.getAllEnemies().size();
                if(count == 0) {
                    continue;
                }
                count = monsterViewModel.getAllMonsters().size();
                if(count == 0) {
                    continue;
                }
                count = monsterViewModel.getAllPlayer().size();
                if(count == 0) {
                    continue;
                }

                break;
            }

            activity = weakReference.get();
            if(activity == null || activity.isFinishing()) {
                return null;
            }

            return "complete";
        }

        @Override
        protected void onPostExecute(String name) {
            super.onPostExecute(name);

            Activity activity = weakReference.get();
            if(activity == null || activity.isFinishing()) {
                return;
            }

            if(name.equals("complete")) {
                notifier.onFinished();
            }
        }
    }
}