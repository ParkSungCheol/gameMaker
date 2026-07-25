package com.example.test;

import android.animation.ValueAnimator;
import android.app.Activity;
import android.app.Dialog;
import android.content.Intent;
import android.content.pm.ActivityInfo;
import android.graphics.drawable.Drawable;
import android.os.Build;
import android.os.CountDownTimer;
import java.util.HashMap;
import java.util.Iterator;
import java.util.Map;
import java.util.Timer;
import android.animation.ValueAnimator.AnimatorUpdateListener;
import android.os.Bundle;
import android.os.Handler;
import android.os.Message;
import android.view.Gravity;
import android.view.View;
import android.view.View.OnClickListener;
import android.view.animation.Animation;
import android.view.animation.AnimationUtils;
import android.view.animation.LinearInterpolator;
import android.widget.Button;
import android.widget.FrameLayout;
import android.widget.ImageView;
import android.widget.FrameLayout.LayoutParams;
import android.widget.LinearLayout;
import android.widget.TextView;
import androidx.annotation.Nullable;
import androidx.annotation.RequiresApi;
import androidx.appcompat.app.AppCompatActivity;
import androidx.lifecycle.ViewModelProvider;
import androidx.vectordrawable.graphics.drawable.Animatable2Compat;
import com.bumptech.glide.Glide;
import com.bumptech.glide.RequestManager;
import com.bumptech.glide.load.DataSource;
import com.bumptech.glide.load.engine.DiskCacheStrategy;
import com.bumptech.glide.load.engine.GlideException;
import com.bumptech.glide.load.resource.gif.GifDrawable;
import com.bumptech.glide.request.RequestListener;
import com.bumptech.glide.request.RequestOptions;
import com.bumptech.glide.request.target.Target;
import java.util.ArrayList;
import java.util.TimerTask;

public class BattlefieldActivity extends AppCompatActivity {

    // MVVM
    private MonsterViewModel monsterViewModel;

    // 아군 적군을 저장할 ArrayList : 0은 아군 / 1은 적군
    ArrayList<Monster>[] party = new ArrayList[]{new ArrayList<Monster>(), new ArrayList<Monster>()};
    ArrayList<Monster>[] removeArray = new ArrayList[]{new ArrayList<Monster>(), new ArrayList<Monster>()};

    // 가장 앞서 있는 Monster 객체 : 0은 아군 / 1은 적군
    Monster[] mostParty = new Monster[]{null, null};

    // container
    FrameLayout frameLayout = null;

    // 화면 너비
    double width = 0.0;

    // custom dialog
    Dialog dialog = null;

    // fade_out animation
    Animation animFadeOut = null;
    TextView fade = null;

    // cost
    ObjectTimer objectTimer = null;
    ImageView costUpgrade = null;
    TextView costView = null;
    TextView costLevel = null;
    TextView costRequire = null;

    // enemy Map
    Map<Integer, String> map_enemy = new HashMap<Integer, String>();

    // time text
    TextView timeText = null;

    // enemy timer
    int enemyCount = 180;
    int end = 0;

    private RequestManager glide = null;

    int value;

    void runnableByName(String name) {
        class OneShotTask implements Runnable {
            String name;
            OneShotTask(String name) { this.name = name; }
            @RequiresApi(api = Build.VERSION_CODES.N)
            public void run() {
                if(name.indexOf("your") >= 0) {
                    monsterViewModel.addYourNewMonster(name);
                }
                else if(name.indexOf("clear") >= 0) {
                    if(name.indexOf("win") >= 0) {
                        monsterViewModel.clear("A", value);
                    }
                    Intent intent = new Intent(BattlefieldActivity.this, MapActivity.class);
                    startActivity(intent);
                    finish();
                }
                else{
                    monsterViewModel.addOurNewMonster(name);
                }
            }
        }
        Thread t = new Thread(new OneShotTask(name));
        t.start();
    }

    void runnableByMapNumber(int mapNumber) {
        class OneShotTask implements Runnable {
            int mapNumber;
            OneShotTask(int mapNumber) { this.mapNumber = mapNumber; }
            @RequiresApi(api = Build.VERSION_CODES.N)
            public void run() {
                monsterViewModel.getEnemiesByMapNumber(mapNumber);
            }
        }
        Thread t = new Thread(new OneShotTask(mapNumber));
        t.start();
    }

    @RequiresApi(api = Build.VERSION_CODES.N)
    public void onCreate(Bundle savedInstanceState) {

        super.onCreate(savedInstanceState);

        glide = Glide.with(this);

        // activity_main xml을 view로 사용
        setContentView(R.layout.battlefield);

        // set MapNumber
        Bundle b = getIntent().getExtras();
        value = b.getInt("key");

        // MVVM
        monsterViewModel = new ViewModelProvider(this, new MonsterViewModelFactory(this.getApplication())).get(MonsterViewModel.class);

        // 캐릭생성 버튼 모여있는 Layout
        LinearLayout buttonView = findViewById(R.id.buttonView);

        // Monster 객체가 동적생성될 FrameLayout
        frameLayout = findViewById(R.id.frameLayout);

        // FrameLayout에 추가적으로 전달할 파라미터
        LayoutParams monster_params = new LayoutParams(LayoutParams.WRAP_CONTENT, LayoutParams.WRAP_CONTENT, Gravity.BOTTOM);
        LayoutParams text_params = new LayoutParams(LayoutParams.WRAP_CONTENT, LayoutParams.WRAP_CONTENT, Gravity.TOP);

        // 화면 너비 값 세팅(absolute)
        width = 2800;

        // custom dialog
        dialog = new Dialog(this);

        // fade_out animation
        animFadeOut = AnimationUtils.loadAnimation(this, R.anim.fade_out);
        // 나타났다 사라지는 textView
        fade = findViewById(R.id.fade);

        LayoutParams new_monster_params = monster_params;
        new_monster_params.rightMargin = (int)(width * 1.05);

        monsterViewModel.getOurNewMonster().observe(this, monster -> {
            if(monster == null) return;
            if(party[0].size() >= 10) {
                alert("10마리를 넘을 수 없습니다.");
                return;
            }
            if(monster.isOur() && objectTimer.getCost() < monster.getCost()) {
                alert("코스트 부족으로 소환할 수 없습니다.");
                return;
            }
            Monster new_monster = CreateMonster(monster);
            objectTimer.setCost(objectTimer.getCost() - monster.getCost());
            // FrameLayout에 이미지 추가
            frameLayout.addView(new_monster.getImageView(), new_monster_params);
            frameLayout.addView(new_monster.getTextView(), text_params);
        });

        monsterViewModel.getYourNewMonster().observe(this, monster -> {
            if(monster == null) return;
            Monster new_monster = CreateMonster(monster);
            // FrameLayout에 이미지 추가
            frameLayout.addView(new_monster.getImageView(), new_monster_params);
            frameLayout.addView(new_monster.getTextView(), text_params);
        });

        monsterViewModel.getEnemies().observe(this, enemies -> {
            if(enemies.size() == 0) return;
            // enemies -> map_enemy(map) CONVERT
            for(Iterator<Enemy> iterator = enemies.iterator(); iterator.hasNext();) {
                Enemy e = iterator.next();
                map_enemy.put(e.getTime(), e.getName());
            }
        });

        runnableByName("ourcastle");
        runnableByName("yourcastle");

        runnableByMapNumber(value);

        // UPGRADE BUTTON 이미지 Array에 저장
        costUpgrade = findViewById(R.id.costUpgrade);
        costView = findViewById(R.id.cost);
        costLevel = findViewById(R.id.costLevel);
        costRequire = findViewById(R.id.costRequire);

        //mapName 부여
        TextView mapName = findViewById(R.id.mapName);
        mapName.setText("STAGE " + value);

        // main thread에게 작업지시
        final Handler handler = new Handler(){
            public void handleMessage(Message msg){
                // 원래 하려던 동작 (UI변경 작업 등)
                if(msg.what == 0) {
                    int level = (210 - objectTimer.getSpeed())/10;
                    costView.setText(objectTimer.getCost() + "원");
                    costLevel.setText("Level : " + level);
                    costRequire.setText(50 * level + "원");
                }
                else if(msg.what == 1) {
                    timeText.setText(enemyCount/60 + ":" + enemyCount%60);
                    if(enemyCount <= 10) {
                        timeText.setTextColor(getResources().getColor(R.color.purple_200));
                    }
                }
            }
        };

        // Cost 타이머 재생 및 CostUpgrade 버튼 Click이벤트 리스너 추가
        int cost = 0;
        int speed = 200;
        int max = 100;
        objectTimer = new ObjectTimer(speed, max, cost);

        Timer timer = new Timer();
        objectTimer.setTimer(timer);

        TimerTask timerTask = new TimerTask() {

            @Override
            public void run() {
                objectTimer.setCost(objectTimer.getCost() + 1);
                // max 초과불가
                if(objectTimer.getCost() > max) {
                    objectTimer.setCost(max);
                }
                // SHOW 현재 코스트
                // obtain 메소드로 메세지 생성
                Message msg = Message.obtain(handler, 0, 0, 0);
                // 메인스레드의 핸들러에 메세지 보내기
                handler.sendMessage(msg);
            }
        };

        objectTimer.setTimerTask(timerTask);
        timer.schedule(timerTask, 0, speed);

        costUpgrade.setOnClickListener(new OnClickListener() {
            public final void onClick(View it) {

                int speed = objectTimer.getSpeed();
                int max = objectTimer.getMax() + 20;
                int current_cost = objectTimer.getCost();
                int level = (210 - objectTimer.getSpeed())/10;
                int cost = 50 * level;
                speed -= 10;

                if(speed < 100) {
                    alert("더 이상 지갑을 업그레이드할 수 없습니다.");
                    return;
                }
                if(current_cost < cost) {
                    alert("지갑업그레이드는 " + cost + " 필요합니다.");
                    return;
                }
                else {
                    objectTimer.setCost(current_cost - cost);
                    objectTimer.setSpeed(speed);
                    objectTimer.setMax(max);

                    TimerTask timerTask = objectTimer.getTimerTask();
                    if(timerTask != null) {
                        timerTask.cancel();
                    }
                    timerTask = new TimerTask() {

                        @Override
                        public void run() {
                            objectTimer.setCost(objectTimer.getCost() + 1);
                            // max 초과불가
                            if(objectTimer.getCost() > max) {
                                objectTimer.setCost(max);
                            }
                            // SHOW 현재 코스트
                            // obtain 메소드로 메세지 생성
                            Message msg = Message.obtain(handler, 0, 0, 0);
                            // 메인스레드의 핸들러에 메세지 보내기
                            handler.sendMessage(msg);
                        }
                    };
                    objectTimer.setTimerTask(timerTask);

                    Timer timer = objectTimer.getTimer();
                    timer.schedule(timerTask, 0, speed);
                }
            }
        });

        // enemy timer(3분 초과 시 over)
        timeText = findViewById(R.id.time);
        Timer enemyTimer = new Timer();
        TimerTask enemyTimerTask = new TimerTask() {
            @Override
            public void run() {
                //main Thread 이용 >> destroyed activity 문제 해결
                BattlefieldActivity.this.runOnUiThread(new Runnable(){
                    public void run() {
                        if(map_enemy.size() >= 1 && map_enemy.containsKey(enemyCount)) {
                            String[] name = map_enemy.get(enemyCount).split(",");
                            map_enemy.remove(enemyCount);
                            for(String e : name) {
                                // obtain 메소드로 메세지 생성
                                // ID String Get
                                String imageName = e;

                                runnableByName(imageName);
                            };
                        }
                        if(end == 0 && enemyCount == 0) {

                            // ID String Get
                            String imageName = "yourboss";

                            // 동적객체 생성
                            runnableByName(imageName);
                            end++;
                        }
                        enemyCount--;
                        if(enemyCount < 0) {
                            enemyCount = 0;
                        }
                        // obtain 메소드로 메세지 생성
                        Message msg = Message.obtain(handler, 1, 0, 0);
                        // 메인스레드의 핸들러에 메세지 보내기
                        handler.sendMessage(msg);
                    }
                });
            }
        };
        enemyTimer.schedule(enemyTimerTask, 0, 1000);

        // 생산 버튼 이미지 / 클릭이벤트 및 Monster 객체 동적생성
        for(int i = 0; i < buttonView.getChildCount(); i++) {
            View subView = buttonView.getChildAt(i);
            if(subView instanceof ImageView) {
                subView.setOnClickListener(new OnClickListener() {
                    public final void onClick(View it) {

                        // ID String Get
                        String imageName = it.getResources().getResourceEntryName(it.getId());

                        // 동적객체 생성
                        runnableByName(imageName);
                    }
                });
            }
        }
    }

    // 앱 종료 로직
    public void closeApp() {
        finishAffinity();
    }

    // 앱 종료 시 실행할 로직
    @RequiresApi(api = Build.VERSION_CODES.N)
    @Override
    protected void onDestroy() {
        super.onDestroy();

        // 모든 리스트 객체 제거
        if(party[0].size() >= 1) {
            party[0].removeAll(party[0]);
        }
        if(party[1].size() >= 1) {
            party[1].removeAll(party[1]);
        }
        if(mostParty[0] != null) {
            remove(mostParty[0], frameLayout, party[1], true, glide, true, BattlefieldActivity.this);
        }
        if(mostParty[1] != null) {
            remove(mostParty[1], frameLayout, party[0], true, glide, false, BattlefieldActivity.this);
        }
        if(objectTimer != null && objectTimer.getTimerTask() != null) {
            objectTimer.getTimerTask().cancel();
        }
    };

    // 안내문구 출력(나왔다가 사라지는 안내문구)
    public void alert(String message) {
        fade.bringToFront();
        fade.setText(message);
        fade.setVisibility(View.VISIBLE);
        fade.startAnimation(animFadeOut);
    }

    @RequiresApi(api = Build.VERSION_CODES.N)
    // 동적으로 이미지 생성(애니메이션 포함)
    public Monster CreateMonster(Monster monster) {

        int base = monster.isOur()? 0 : 1;

        // 안드로이드 버전이 낮아 실행이 불가능한 경우
        if(Build.VERSION.SDK_INT < Build.VERSION_CODES.N) {

            String message = "현재 안드로이드 버전이 N버전보다 낮습니다!";

            // custom dialog
            dialog.setContentView(R.layout.dialog);
            TextView text = dialog.findViewById(R.id.textView);
            text.setText(message);
            Button dialogButton = (Button) dialog.findViewById(R.id.dialogButtonOK);

            // if button is clicked, close the custom dialog
            dialogButton.setOnClickListener(new View.OnClickListener() {
                @Override
                public void onClick(View v) {
                    dialog.dismiss();
                    closeApp();
                }
            });

            dialog.show();
        }

        // name에 해당하는 이미지 동적생성
        ImageView newImg = new ImageView(this);
        newImg.setScaleType(ImageView.ScaleType.CENTER);
        setViewResource(monster.getName(), "move", newImg);

        monster.setImageView(newImg);

        // 텍스트 객체 생성
        TextView textView = new TextView(this);
        textView.setText(monster.getHp() + " / " + monster.getTotal_hp());
        monster.setTextView(textView);

        // 첫 번째 유닛은 가장 앞에 있는 객체에 할당하고 아군 / 적군 유닛 리스트에 추가
        if(monster.isCastle()) {
            mostParty[base] = monster;
            textView.setVisibility(View.VISIBLE);
        }
        else {
            textView.setVisibility(View.INVISIBLE);
        }
        party[base].add(monster);

        // 아군이면 왼쪽에서 오른쪽으로 진행, 적군이면 오른쪽에서 왼쪽으로 진행
        float translationXFrom = base == 0? 25.0F : (float) width;
        float translationXTo = base == 1? 25.0F : (float) width;
        if(monster.isCastle()) {
            translationXFrom = base == 0? 25.0F : (float) width;
            translationXTo = base == 0? 25.0F : (float) width;
        }

        MyValueAnimator myValueAnimator = new MyValueAnimator(translationXFrom, translationXTo);

        // 속도(speed)는 Duration을 다르게 저장하는 방식으로 구현예정(DB에서 speed 값 가져와서)
        myValueAnimator.setDuration(monster.getSpeed());
        monster.setMyValueAnimator(myValueAnimator);

        // 매번 위치가 변경될 때마다 Update 이벤트 인식
        float finalTranslationXFrom = translationXFrom;
        Monster finalNew_monster = monster;
        myValueAnimator.addUpdateListener(new AnimatorUpdateListener() {

            @RequiresApi(api = Build.VERSION_CODES.KITKAT)
            public void onAnimationUpdate(ValueAnimator it) {

                // 각 시점마다 업데이트된 X좌표 위치를 받아와서
                float animatedValue = (float)it.getAnimatedValue();

                if(finalNew_monster.getPercent() != 0 && animatedValue == finalTranslationXFrom) {
                    animatedValue = ((MyValueAnimator)it).animatedValue;
                }

                // 직선보간(기존위치 - 변경위치 간 두 점을 직선으로 연결하여 이동)
                it.setInterpolator(new LinearInterpolator());

                // 해당 이미지를 변경위치로 이동
                newImg.setTranslationX(animatedValue);
                textView.setTranslationX(animatedValue);

                // 해당 이미지 / 가장 앞서 있는 객체의 위치 거리
                float thisX = Math.abs(newImg.getTranslationX() - finalTranslationXFrom);
                float thatX = 0.0f;
                if (mostParty[base] != null) {
                    thatX = Math.abs(mostParty[base].getImageView().getTranslationX() - finalTranslationXFrom);
                }
                float thatYthisX = 0.0f;
                if(mostParty[1-base] != null) {
                    thatYthisX = Math.abs(mostParty[1-base].getImageView().getTranslationX() - newImg.getTranslationX());
                }

                // 성 뒤에 있는 경우 제외
                if((base == 0 && newImg.getTranslationX() < finalTranslationXFrom) || (base == 1 && newImg.getTranslationX() > finalTranslationXFrom)){

                }
                // 가장 앞서 있는 객체보다 더 앞서 있다면
                else if(mostParty[base] != null && thisX > thatX) {

                    // 기존 앞서 있는 객체의 텍스트뷰 invisible
                    mostParty[base].getTextView().setVisibility(View.INVISIBLE);

                    // 가장 앞서 있는 객체 변경
                    mostParty[base] = finalNew_monster;

                    // 새로운 앞서 있는 객체의 텍스트뷰 visible
                    mostParty[base].getTextView().setVisibility(View.VISIBLE);
                }

                // 적군과의 거리가 아군 객체의 인식범위 안이라면
                if(mostParty[1-base] != null && thatYthisX <= finalNew_monster.getRecognize_range()) {

                    // 이동을 멈춤
                    it.pause();

                    // 움직이는 gif에서 공격하는 gif로 변경
                    setViewResource(finalNew_monster.getName(), "attack", newImg);

                    // 기존 공격 이벤트가 없다면
                    if(finalNew_monster.getCountDownTimer() == null) {

                        // 공격 이벤트 시작
                        finalNew_monster.setCountDownTimer(new CountDownTimer(Long.parseLong("9999999999999999"), finalNew_monster.getAttack_speed()) {

                            // 매 공격속도마다
                            @RequiresApi(api = Build.VERSION_CODES.N)
                            public void onTick(long millisUntilFinished) {

                                if(mostParty[1-base] != null) {

                                    // 상성에 따른 데미지 multiply
                                    float multiply = 1.0f;
                                    int thisType = finalNew_monster.getType();
                                    int thatType = mostParty[1-base].getType();
                                    if(Math.abs(thisType - thatType) < 2) {
                                        if(thisType > thatType) {
                                            multiply = 2.0f;
                                        }
                                        else if(thisType < thatType) {
                                            multiply = 0.5f;
                                        }
                                    }
                                    else {
                                        if(thisType > thatType) {
                                            multiply = 0.5f;
                                        }
                                        else if(thisType < thatType) {
                                            multiply = 2.0f;
                                        }
                                    }
                                    // 단일
                                    if(finalNew_monster.getAttack_style() == 0 && party[1-base].size() >= 1) {
                                        mostParty[1-base].setHp(mostParty[1-base].getHp() - (int)(finalNew_monster.getAttack() * multiply));
                                        mostParty[1-base].getTextView().setText(mostParty[1-base].getHp() + " / " + mostParty[1-base].getTotal_hp());

                                        // 만약 가장 앞서 있는 객체의 hp가 0보다 작아진다면
                                        if(mostParty[1-base].getHp() <= 0) {
                                            mostParty[1-base].getTextView().setText("0 / " + mostParty[1-base].getTotal_hp());
                                            onDelete(mostParty[1-base]);
                                            onFinish();
                                        }

                                        else if(getPercent(finalNew_monster.getPercent()) && !mostParty[1 - base].isCastle()) {

                                            int move = base == 0? 500 : - 500;
                                            float totalLength = (float)(width - 25.0);

                                            MyValueAnimator it = mostParty[1 - base].getMyValueAnimator();

                                            // 각 시점마다 업데이트된 X좌표 위치를 받아와서
                                            float animatedValueFrom = (float)it.getAnimatedValue() + move;
                                            if(base == 0 && animatedValueFrom > (float)width + 300.0f) {
                                                animatedValueFrom = (float)width + 300.0f;
                                            }
                                            else if(base == 1 && animatedValueFrom < -275.0f) {
                                                animatedValueFrom = -275.0f;
                                            }
                                            float animatedValueTo = base == 0? 25.0f : (float) width;
                                            float remainValue = Math.abs(animatedValueFrom - animatedValueTo) / totalLength;

                                            // 직선보간(기존위치 - 변경위치 간 두 점을 직선으로 연결하여 이동)
                                            it.end();
                                            it.setFloatValues(animatedValueFrom, animatedValueTo);
                                            it.setDuration((long)(mostParty[1 - base].getSpeed()*remainValue));
                                            it.setAnimatedValue(animatedValueFrom);
                                            it.setCurrenttime(0L);
                                            it.setTotaltime(10000L);
                                            // 해당 이미지를 변경위치로 이동
                                            mostParty[1 - base].getImageView().setTranslationX(animatedValueFrom);
                                            mostParty[1 - base].getTextView().setTranslationX(animatedValueFrom);

                                            it.start();

                                            // 기존 앞서 있는 객체의 텍스트뷰 INVISIBLE
                                            mostParty[1 - base].getTextView().setVisibility(View.INVISIBLE);

                                            mostParty[1 - base] = party[1 - base].get(0);

                                            // 적군 가장 앞서 있는 객체 재설정
                                            if (party[1 - base].size() >= 2) {

                                                for(Iterator<Monster> iterator = party[1 - base].iterator(); iterator.hasNext();) {
                                                    Monster e = iterator.next();
                                                    float thatXthisX = mostParty[1 - base].getImageView().getTranslationX() - e.getImageView().getTranslationX();
                                                    if ((thatXthisX > 0 && base == 0) || (thatXthisX < 0 && base == 1)) {
                                                        mostParty[1 - base] = e;
                                                    }
                                                };
                                            }
                                            // 새로운 앞서 있는 객체의 텍스트뷰 visible
                                            mostParty[1 - base].getTextView().setVisibility(View.VISIBLE);
                                        }
                                    }
                                    else if (party[1-base].size() >= 1) {
                                        Monster X = null;
                                        // 범위
                                        if(finalNew_monster.getAttack_style() == 1) {
                                            X = finalNew_monster;
                                        }
                                        // 원거리 범위
                                        else {
                                            X = mostParty[1-base];
                                        }
                                        float betweenX = X.getImageView().getTranslationX();
                                        float finalMultiply = multiply;
                                        for(Iterator<Monster> iterator = party[1 - base].iterator(); iterator.hasNext();) {
                                            Monster e = iterator.next();
                                            if(e.getHp()<=0) continue;
                                            float betweenY = Math.abs(e.getImageView().getTranslationX() - betweenX);
                                            if(betweenY <= finalNew_monster.getAttack_range()) {
                                                e.setHp(e.getHp() - (int)(finalNew_monster.getAttack() * finalMultiply));
                                                e.getTextView().setText(e.getHp() + " / " + e.getTotal_hp());

                                                // 만약 공격받은 객체의 hp가 0보다 작아진다면
                                                if(e.getHp() <= 0) {
                                                   e.getTextView().setText("0 / " + e.getTotal_hp());
                                                    onDelete(e);
                                                }

                                                else if(getPercent(finalNew_monster.getPercent()) && !e.isCastle()) {
                                                    int move = base == 0? 500 : - 500;
                                                    float totalLength = (float)(width - 25.0);

                                                    MyValueAnimator it = e.getMyValueAnimator();

                                                    // 각 시점마다 업데이트된 X좌표 위치를 받아와서
                                                    float animatedValueFrom = (float)it.getAnimatedValue() + move;
                                                    if(base == 0 && animatedValueFrom > (float)width + 300.0f) {
                                                        animatedValueFrom = (float)width + 300.0f;
                                                    }
                                                    else if(base == 1 && animatedValueFrom < -275.0f) {
                                                        animatedValueFrom = -275.0f;
                                                    }
                                                    float animatedValueTo = base == 0? 25.0f : (float) width;
                                                    float remainValue = Math.abs(animatedValueFrom - animatedValueTo) / totalLength;

                                                    // 직선보간(기존위치 - 변경위치 간 두 점을 직선으로 연결하여 이동)
                                                    it.end();
                                                    it.setFloatValues(animatedValueFrom, animatedValueTo);
                                                    it.setDuration((long)(e.getSpeed()*remainValue));
                                                    it.setAnimatedValue(animatedValueFrom);
                                                    it.setCurrenttime(0L);
                                                    it.setTotaltime(10000L);
                                                    // 해당 이미지를 변경위치로 이동
                                                    e.getImageView().setTranslationX(animatedValueFrom);
                                                    e.getTextView().setTranslationX(animatedValueFrom);

                                                    it.start();

                                                    if(e == mostParty[1 - base]) {
                                                        // 기존 앞서 있는 객체의 텍스트뷰 INVISIBLE
                                                        mostParty[1 - base].getTextView().setVisibility(View.INVISIBLE);

                                                        mostParty[1 - base] = party[1 - base].get(0);

                                                        // 적군 가장 앞서 있는 객체 재설정
                                                        if (party[1 - base].size() >= 2) {

                                                            for(Iterator<Monster> newIterator = party[1 - base].iterator(); newIterator.hasNext();) {
                                                                Monster i = newIterator.next();
                                                                float thatXthisX = mostParty[1 - base].getImageView().getTranslationX() - i.getImageView().getTranslationX();
                                                                if ((thatXthisX > 0 && base == 0) || (thatXthisX < 0 && base == 1)) {
                                                                    mostParty[1 - base] = i;
                                                                }
                                                            };
                                                        }
                                                        // 새로운 앞서 있는 객체의 텍스트뷰 visible
                                                        mostParty[1 - base].getTextView().setVisibility(View.VISIBLE);
                                                    }
                                                }
                                            }
                                        };

                                        party[1 - base].removeAll(removeArray[1-base]);
                                        removeArray[1-base].clear();
                                    }
                                }
                                else {
                                    onFinish();
                                }

                            }

                            // 종료로직
                            @Override
                            public void onFinish() {
                                cancel();
                            }

                            @RequiresApi(api = Build.VERSION_CODES.N)
                            public void onDelete(Monster monster) {
                                if (mostParty[1 - base] != null && monster.isCastle()) {

                                    String message = base == 0 ? "승리하셨습니다" : "패배하셨습니다";

                                    // custom dialog
                                    dialog.setContentView(R.layout.dialog);
                                    dialog.setCancelable(false);
                                    TextView text = dialog.findViewById(R.id.textView);
                                    text.setText(message);
                                    Button dialogButton = (Button) dialog.findViewById(R.id.dialogButtonOK);

                                    // if button is clicked, close the custom dialog
                                    dialogButton.setOnClickListener(new View.OnClickListener() {
                                        @Override
                                        public void onClick(View v) {
                                            runnableByName(base == 0? "clearwin" : "cleardefeat");
                                            dialog.dismiss();
                                            onFinish();
                                            mostParty[1 - base] = null;
                                        }
                                    });

                                    dialog.show();
                                }
                                else if(mostParty[1 - base] != null && party[1 - base].size() >= 1) {

                                    remove(monster, frameLayout, party[base], true, glide, base == 1, BattlefieldActivity.this);
                                    removeArray[1-base].add(monster);

                                    ArrayList<Monster> tempList = (ArrayList<Monster>)party[1 - base].clone();
                                    tempList.removeAll(removeArray[1-base]);
                                    mostParty[1 - base] = tempList.get(0);

                                    // 적군 가장 앞서 있는 객체 재설정
                                    if (tempList.size() >= 2) {

                                        for (Iterator<Monster> iterator = party[1 - base].iterator(); iterator.hasNext();) {
                                            Monster e = iterator.next();
                                            if(e.getHp() <= 0) continue;
                                            float thatXthisX = mostParty[1 - base].getImageView().getTranslationX() - e.getImageView().getTranslationX();
                                            if ((thatXthisX > 0 && base == 0) || (thatXthisX < 0 && base == 1)) {
                                                mostParty[1 - base] = e;
                                            }
                                        };
                                    }

                                    // 새로운 앞서 있는 객체의 텍스트뷰 visible
                                    mostParty[1 - base].getTextView().setVisibility(View.VISIBLE);
                                }
                            }
                        }.start());
                    }
                }
                else if(mostParty[1-base] != null && thatYthisX > finalNew_monster.getRecognize_range() && !finalNew_monster.isCastle()){
                    // 이동 재개
                    it.resume();
                    // 공격하는 gif에서 움직이는 gif로 변경
                    setViewResource(finalNew_monster.getName(), "move", newImg);

                    if(finalNew_monster.getCountDownTimer() != null) {
                        finalNew_monster.getCountDownTimer().onFinish();
                        finalNew_monster.setCountDownTimer(null);
                    }
                }
            }});

        myValueAnimator.start();

        return monster;
    }

    // 확률 부여 75% -> 75 input
    public boolean getPercent(int num) {
        double dValue = Math.random()*100;
        return dValue < num;
    }

    public void setViewResource(String name, String action, ImageView newImg) {
        int checkExistence = getResources().getIdentifier(name + action, "drawable", getPackageName());
        if ( checkExistence != 0 ) {  // the resource exists
            glide.load(getResources().getIdentifier(name + action , "drawable", getApplicationContext().getPackageName())).into(newImg);
        }
        else {  // checkExistence == 0  // the resource does NOT exist
            newImg.setImageResource(getResources().getIdentifier(name, "drawable", getApplicationContext().getPackageName()));
        }
    }

    // 객체가 제거될 시 실행할 로직
    @RequiresApi(api = Build.VERSION_CODES.N)
    public void remove(Monster monster, FrameLayout frameLayout, ArrayList<Monster> array, boolean isFront, RequestManager requestManager, boolean isOur, Activity activity) {

        if(monster.getCountDownTimer() != null) {

            monster.getCountDownTimer().onFinish();
            monster.setCountDownTimer(null);
        }

        if(monster.getMyValueAnimator() != null) {
            if(!monster.isOur()) {
                objectTimer.setCost(objectTimer.getCost() + monster.getCost());
            }
            monster.getMyValueAnimator().removeAllListeners(); // Or animator.removeListener(your listener);
            monster.getMyValueAnimator().cancel();
        }

        if(isFront && array.size() >= 1) {

            for (Iterator<Monster> iterator = array.iterator(); iterator.hasNext();) {
                Monster e = iterator.next();

                if (e.getCountDownTimer() != null) {

                    e.getCountDownTimer().onFinish();
                    e.setCountDownTimer(null);
                }

            };
        }

        frameLayout.removeView(monster.getTextView());

        int gif = activity.getResources().getIdentifier(monster.getName() + "defeat", "drawable", activity.getPackageName());
        try {
            // 죽는 gif로 한 번 실행 후 이미지 제거
            requestManager.asGif()
                    .load(gif) //Your gif resource
                    .apply(RequestOptions.diskCacheStrategyOf(DiskCacheStrategy.AUTOMATIC))
                    .listener(new RequestListener<GifDrawable>() {
                        @Override
                        public boolean onLoadFailed(@Nullable @org.jetbrains.annotations.Nullable GlideException e, Object model, Target<GifDrawable> target, boolean isFirstResource) {
                            return false;
                        }

                        @Override
                        public boolean onResourceReady(GifDrawable resource, Object model, Target<GifDrawable> target, DataSource dataSource, boolean isFirstResource) {
                            resource.setLoopCount(1);
                            resource.registerAnimationCallback(new Animatable2Compat.AnimationCallback() {
                                @Override
                                public void onAnimationEnd(Drawable drawable) {
                                    //do whatever after specified number of loops complete
                                    frameLayout.removeView(monster.getImageView());
                                }
                            });
                            return false;
                        }
                    })
                    .into(monster.getImageView());
        }
        catch(Exception e) {  // checkExistence == 0  // the resource does NOT exist
            frameLayout.removeView(monster.getImageView());
        }
    }
}